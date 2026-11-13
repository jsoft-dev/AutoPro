#!/usr/bin/env python3
"""AutoPro local dev launcher — browser UI for .NET API, WebApp, and Flutter CR."""

from __future__ import annotations

import json
import os
import platform
import shutil
import signal
import socket
import subprocess
import sys
import threading
import urllib.error
import urllib.request
import webbrowser
from dataclasses import dataclass, field
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from typing import Callable

REPO_ROOT = Path(__file__).resolve().parents[2]
LAUNCHER_DIR = Path(__file__).resolve().parent
LAUNCHER_PORT = 8765
POLL_SECONDS = 2
MOBILE_DIR = REPO_ROOT / "mobile" / "autopro_cr"
HOSTED_API_URL = "https://autopro-api-6mkp.onrender.com"
APK_OUTPUT = MOBILE_DIR / "build" / "app" / "outputs" / "flutter-apk" / "app-release.apk"

_EXTRA_PATH_DIRS = [
    Path.home() / "flutter" / "bin",
    Path.home() / "development" / "flutter" / "bin",
    Path.home() / ".fvm" / "default" / "bin",
    Path("/opt/homebrew/bin"),
    Path("/usr/local/bin"),
    Path("/usr/local/share/dotnet"),
    Path.home() / ".dotnet" / "tools",
]

_TOOLS: dict[str, str] = {}


def _load_path_overrides() -> dict[str, str]:
    cfg = LAUNCHER_DIR / "paths.local.json"
    if not cfg.is_file():
        return {}
    try:
        data = json.loads(cfg.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, OSError):
        return {}
    if not isinstance(data, dict):
        return {}
    return {str(k): str(v) for k, v in data.items() if isinstance(k, str) and isinstance(v, str)}


def launcher_env() -> dict[str, str]:
    env = os.environ.copy()
    prepend = os.pathsep.join(str(d) for d in _EXTRA_PATH_DIRS if d.is_dir())
    existing = env.get("PATH", "")
    if prepend:
        env["PATH"] = prepend + (os.pathsep + existing if existing else "")
    if not env.get("ANDROID_HOME"):
        sdk = Path.home() / "Library" / "Android" / "sdk"
        if sdk.is_dir():
            env["ANDROID_HOME"] = str(sdk)
            platform_tools = sdk / "platform-tools"
            if platform_tools.is_dir():
                env["PATH"] = str(platform_tools) + os.pathsep + env.get("PATH", "")
    return env


def resolve_tool(name: str) -> str:
    if name in _TOOLS:
        return _TOOLS[name]
    overrides = _load_path_overrides()
    if name in overrides and Path(overrides[name]).is_file():
        _TOOLS[name] = overrides[name]
        return overrides[name]

    env = launcher_env()
    found = shutil.which(name, path=env.get("PATH"))
    if found:
        _TOOLS[name] = found
        return found

    for directory in _EXTRA_PATH_DIRS:
        candidate = directory / name
        if candidate.is_file() and os.access(candidate, os.X_OK):
            _TOOLS[name] = str(candidate)
            return str(candidate)

    raise FileNotFoundError(
        f"{name} not found on PATH. Install it or set \"{name}\" in "
        f"{LAUNCHER_DIR / 'paths.local.json'}"
    )


def prefetch_tools() -> list[str]:
    """Resolve dev CLIs; return human-readable warnings for any missing."""
    warnings: list[str] = []
    for name in ("dotnet", "npm", "flutter"):
        try:
            resolve_tool(name)
        except FileNotFoundError as exc:
            warnings.append(str(exc))
    return warnings

STATUS_STOPPED = "Stopped"
STATUS_STARTING = "Starting…"
STATUS_RUNNING = "Running"

_LOG_LOCK = threading.Lock()
_LOG_LINES: list[str] = []
_MAX_LOG_LINES = 400


def _log(line: str) -> None:
    with _LOG_LOCK:
        for part in line.splitlines(keepends=True):
            _LOG_LINES.append(part)
        while len(_LOG_LINES) > _MAX_LOG_LINES:
            _LOG_LINES.pop(0)


def port_open(port: int, host: str = "127.0.0.1", timeout: float = 0.75) -> bool:
    try:
        with socket.create_connection((host, port), timeout=timeout):
            return True
    except OSError:
        return False


def http_reachable(url: str, timeout: float = 1.5) -> bool:
    try:
        req = urllib.request.Request(url, method="GET")
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            return 200 <= resp.status < 500
    except (urllib.error.URLError, TimeoutError, OSError, ValueError):
        return False


def web_dev_server_up(port: int, *urls: str) -> bool:
    if port_open(port):
        return True
    return any(http_reachable(url) for url in urls)


def pids_listening_on(port: int) -> list[int]:
    try:
        out = subprocess.run(
            ["lsof", "-ti", f":{port}"],
            capture_output=True,
            text=True,
            check=False,
        )
    except FileNotFoundError:
        return []
    return [int(line.strip()) for line in out.stdout.splitlines() if line.strip().isdigit()]


def kill_pids(pids: list[int], grace_seconds: float = 2.0) -> None:
    for pid in pids:
        try:
            os.kill(pid, signal.SIGTERM)
        except ProcessLookupError:
            pass
    if not pids:
        return

    def _wait() -> None:
        import time

        time.sleep(grace_seconds)
        for pid in pids:
            try:
                os.kill(pid, 0)
            except ProcessLookupError:
                continue
            try:
                os.kill(pid, signal.SIGKILL)
            except ProcessLookupError:
                pass

    threading.Thread(target=_wait, daemon=True).start()


def default_flutter_device() -> list[str]:
    try:
        flutter = resolve_tool("flutter")
    except FileNotFoundError:
        return ["-d", "macos" if platform.system() == "Darwin" else "chrome"]

    try:
        proc = subprocess.run(
            [flutter, "devices", "--machine"],
            cwd=REPO_ROOT / "mobile" / "autopro_cr",
            capture_output=True,
            text=True,
            check=False,
            timeout=30,
            env=launcher_env(),
        )
    except subprocess.TimeoutExpired:
        return ["-d", "macos" if platform.system() == "Darwin" else "chrome"]

    try:
        devices = json.loads(proc.stdout or "[]")
    except json.JSONDecodeError:
        devices = []

    # Camera QR scanning needs a phone or Android emulator — not macOS/Chrome.
    preferred = ("android", "ios", "macos", "chrome")
    for kind in preferred:
        for dev in devices:
            if not isinstance(dev, dict):
                continue
            target = str(dev.get("targetPlatform", "")).lower()
            name = str(dev.get("name", "")).lower()
            if kind in target or kind in name:
                dev_id = dev.get("id")
                if dev_id:
                    return ["-d", str(dev_id)]

    for dev in devices:
        if isinstance(dev, dict) and dev.get("id"):
            return ["-d", str(dev["id"])]

    return ["-d", "macos" if platform.system() == "Darwin" else "chrome"]


@dataclass
class ServiceSpec:
    key: str
    title: str
    subtitle: str
    url: str | None
    check_running: Callable[[], bool]
    build_command: Callable[[], list[str]]
    cwd: Path
    stop_ports: list[int] = field(default_factory=list)
    stop_match: str | None = None


@dataclass
class ManagedProcess:
    proc: subprocess.Popen | None = None


class DevLauncher:
    def __init__(self) -> None:
        self._lock = threading.Lock()
        self._processes: dict[str, ManagedProcess] = {
            "api": ManagedProcess(),
            "admin": ManagedProcess(),
            "mobile": ManagedProcess(),
        }
        self._apk_building = False
        self._specs = self._build_specs()

    def _build_specs(self) -> dict[str, ServiceSpec]:
        api_proj = REPO_ROOT / "src" / "AutoPro.Api" / "AutoPro.Api.csproj"
        admin_dir = REPO_ROOT / "web" / "autopro_admin"
        mobile_dir = MOBILE_DIR

        return {
            "api": ServiceSpec(
                key="api",
                title=".NET API",
                subtitle="AutoPro.Api",
                url="http://localhost:5080/swagger",
                check_running=lambda: web_dev_server_up(
                    5080,
                    "http://127.0.0.1:5080/swagger",
                    "http://127.0.0.1:5080/",
                ),
                build_command=lambda: [
                    resolve_tool("dotnet"),
                    "run",
                    "--project",
                    str(api_proj),
                ],
                cwd=REPO_ROOT,
                stop_ports=[5080],
            ),
            "admin": ServiceSpec(
                key="admin",
                title="WebApp",
                subtitle="The WebApp — jobs, vehicles, inventory",
                url="http://localhost:4200",
                check_running=lambda: web_dev_server_up(
                    4200,
                    "http://127.0.0.1:4200/",
                ),
                build_command=lambda: [resolve_tool("npm"), "start"],
                cwd=admin_dir,
                stop_ports=[4200],
            ),
            "mobile": ServiceSpec(
                key="mobile",
                title="Flutter CR",
                subtitle="Customer rep mobile app",
                url=None,
                check_running=self._flutter_running,
                build_command=lambda: [
                    resolve_tool("flutter"),
                    "run",
                    *default_flutter_device(),
                    "--dart-define=API_BASE_URL=http://localhost:5080",
                ],
                cwd=mobile_dir,
                stop_match="flutter.*autopro_cr",
            ),
        }

    def _flutter_running(self) -> bool:
        mp = self._processes["mobile"]
        if mp.proc and mp.proc.poll() is None:
            return True
        mobile_dir = REPO_ROOT / "mobile" / "autopro_cr"
        markers = ("autopro_cr", str(mobile_dir), "autopro_cr.app")
        for pattern in ("autopro_cr", "flutter", "Runner"):
            try:
                out = subprocess.run(
                    ["pgrep", "-fl", pattern],
                    capture_output=True,
                    text=True,
                    check=False,
                )
            except FileNotFoundError:
                continue
            hay = out.stdout
            if not hay.strip():
                continue
            if pattern == "flutter" and "flutter_tools" not in hay and " run" not in hay:
                continue
            if pattern == "autopro_cr" or any(m in hay for m in markers):
                return True
        return False

    def service_status(self, key: str) -> dict:
        spec = self._specs[key]
        mp = self._processes[key]

        with self._lock:
            if mp.proc is not None and mp.proc.poll() is not None:
                mp.proc = None

            live = spec.check_running()
            launching = mp.proc is not None and mp.proc.poll() is None

            if live:
                label = STATUS_RUNNING
                running = True
            elif launching:
                label = STATUS_STARTING
                running = False
            else:
                label = STATUS_STOPPED
                running = False

        payload = {
            "key": key,
            "title": spec.title,
            "subtitle": spec.subtitle,
            "url": spec.url,
            "status": label,
            "running": running,
        }
        if key == "mobile":
            with self._lock:
                building = self._apk_building
            payload["apk_building"] = building
            payload["apk_path"] = str(APK_OUTPUT) if APK_OUTPUT.is_file() else None
            payload["hosted_api_url"] = HOSTED_API_URL
        return payload

    def all_status(self) -> dict:
        return {
            "repo": str(REPO_ROOT),
            "services": [self.service_status(k) for k in ("api", "admin", "mobile")],
        }

    def build_apk_hosted(self) -> None:
        with self._lock:
            if self._apk_building:
                _log("[Flutter CR APK] Build already in progress.\n")
                return
            self._apk_building = True

        try:
            flutter = resolve_tool("flutter")
        except FileNotFoundError as exc:
            with self._lock:
                self._apk_building = False
            _log(f"[Flutter CR APK] Failed: {exc}\n")
            return

        cmd = [
            flutter,
            "build",
            "apk",
            "--release",
            f"--dart-define=API_BASE_URL={HOSTED_API_URL}",
        ]
        _log(f"[Flutter CR APK] $ {' '.join(cmd)}\n")
        _log(f"[Flutter CR APK] API → {HOSTED_API_URL}\n")

        def run_in_thread() -> None:
            code = 1
            try:
                proc = subprocess.run(
                    cmd,
                    cwd=MOBILE_DIR,
                    stdout=subprocess.PIPE,
                    stderr=subprocess.STDOUT,
                    text=True,
                    env=launcher_env(),
                )
                code = proc.returncode or 0
                for line in (proc.stdout or "").splitlines(keepends=True):
                    _log(line)
            except FileNotFoundError as exc:
                _log(f"[Flutter CR APK] Failed: command not found ({exc}).\n")
            except OSError as exc:
                _log(f"[Flutter CR APK] Failed: {exc}\n")
            else:
                if code == 0 and APK_OUTPUT.is_file():
                    _log(f"[Flutter CR APK] Done → {APK_OUTPUT}\n")
                else:
                    _log(f"[Flutter CR APK] Finished with exit code {code}.\n")
            with self._lock:
                self._apk_building = False

        threading.Thread(target=run_in_thread, daemon=True).start()

    def start(self, key: str) -> None:
        spec = self._specs[key]
        mp = self._processes[key]

        with self._lock:
            if mp.proc and mp.proc.poll() is None:
                _log(f"[{spec.title}] Already starting (managed process).\n")
                return
            if spec.check_running():
                _log(f"[{spec.title}] Already running; use Restart to relaunch.\n")
                return
        try:
            cmd = spec.build_command()
        except FileNotFoundError as exc:
            _log(f"[{spec.title}] Failed: {exc}\n")
            return

        _log(f"[{spec.title}] $ {' '.join(cmd)}\n")

        def run_in_thread() -> None:
            try:
                proc = subprocess.Popen(
                    cmd,
                    cwd=spec.cwd,
                    stdout=subprocess.PIPE,
                    stderr=subprocess.STDOUT,
                    text=True,
                    bufsize=1,
                    start_new_session=True,
                    env=launcher_env(),
                )
                with self._lock:
                    mp.proc = proc
                assert proc.stdout is not None
                for line in proc.stdout:
                    _log(line if line.endswith("\n") else line + "\n")
                proc.wait()
                with self._lock:
                    mp.proc = None
                _log(f"[{spec.title}] Process exited (code {proc.returncode}).\n")
            except FileNotFoundError as exc:
                with self._lock:
                    mp.proc = None
                _log(f"[{spec.title}] Failed: command not found ({exc}).\n")
            except OSError as exc:
                with self._lock:
                    mp.proc = None
                _log(f"[{spec.title}] Failed: {exc}\n")

        threading.Thread(target=run_in_thread, daemon=True).start()

    def stop(self, key: str) -> None:
        spec = self._specs[key]
        mp = self._processes[key]
        _log(f"[{spec.title}] Stopping…\n")

        with self._lock:
            if mp.proc and mp.proc.poll() is None:
                try:
                    os.killpg(os.getpgid(mp.proc.pid), signal.SIGTERM)
                except (ProcessLookupError, OSError):
                    try:
                        mp.proc.terminate()
                    except OSError:
                        pass
                mp.proc = None
        for port in spec.stop_ports:
            kill_pids(pids_listening_on(port))

        if spec.stop_match:
            try:
                out = subprocess.run(
                    ["pgrep", "-f", spec.stop_match],
                    capture_output=True,
                    text=True,
                    check=False,
                )
                pids = [int(x) for x in out.stdout.split() if x.strip().isdigit()]
                kill_pids(pids)
            except FileNotFoundError:
                pass

    def restart(self, key: str) -> None:
        self.stop(key)

        def delayed() -> None:
            import time

            time.sleep(0.8)
            self.start(key)

        threading.Thread(target=delayed, daemon=True).start()


LAUNCHER = DevLauncher()


def index_html() -> str:
    poll_ms = POLL_SECONDS * 1000
    return f"""<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>AutoPro Dev Launcher</title>
  <style>
    :root {{
      --bg: #0f1114;
      --card: #1a1d23;
      --border: #2c3139;
      --text: #e8eaed;
      --muted: #9aa0a6;
      --ok: #3dd68c;
      --warn: #f5a524;
      --bad: #f07178;
      --btn: #2b313a;
      --btn-hover: #353c47;
      --accent: #6c9eff;
    }}
    * {{ box-sizing: border-box; }}
    body {{
      margin: 0;
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
      background: var(--bg);
      color: var(--text);
      line-height: 1.45;
    }}
    .page {{ max-width: 720px; margin: 0 auto; padding: 24px 20px 40px; }}
    h1 {{ font-size: 1.35rem; font-weight: 600; margin: 0 0 4px; }}
    .repo {{ color: var(--muted); font-size: 0.8rem; margin: 0 0 20px; word-break: break-all; }}
    .cards {{ display: flex; flex-direction: column; gap: 12px; margin-bottom: 20px; }}
    .card {{
      background: var(--card);
      border: 1px solid var(--border);
      border-radius: 10px;
      padding: 14px 16px;
    }}
    .card-head {{ display: flex; justify-content: space-between; align-items: flex-start; gap: 12px; }}
    .card h3 {{ margin: 0; font-size: 1rem; font-weight: 600; }}
    .sub {{ color: var(--muted); font-size: 0.82rem; margin-top: 2px; }}
    .link {{ color: var(--accent); font-size: 0.82rem; text-decoration: none; }}
    .link:hover {{ text-decoration: underline; }}
    .pill {{
      font-size: 0.72rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      padding: 4px 8px;
      border-radius: 6px;
      white-space: nowrap;
    }}
    .pill.running {{ background: rgba(61, 214, 140, 0.15); color: var(--ok); }}
    .pill.starting {{ background: rgba(245, 165, 36, 0.15); color: var(--warn); }}
    .pill.stopped {{ background: rgba(240, 113, 120, 0.12); color: var(--bad); }}
    .actions {{ display: flex; gap: 8px; margin-top: 12px; flex-wrap: wrap; }}
    button {{
      appearance: none;
      border: 1px solid var(--border);
      background: var(--btn);
      color: var(--text);
      border-radius: 8px;
      padding: 8px 14px;
      font-size: 0.85rem;
      cursor: pointer;
    }}
    button:hover {{ background: var(--btn-hover); }}
    button:disabled {{ opacity: 0.45; cursor: not-allowed; }}
    .log-panel h2 {{ font-size: 0.9rem; margin: 0 0 8px; color: var(--muted); }}
    #log {{
      margin: 0;
      background: #0a0c0f;
      border: 1px solid var(--border);
      border-radius: 10px;
      padding: 12px;
      height: 220px;
      overflow: auto;
      font-family: ui-monospace, Menlo, monospace;
      font-size: 11px;
      color: #c4c8cc;
      white-space: pre-wrap;
    }}
  </style>
</head>
<body>
  <main class="page">
    <h1>AutoPro Dev Launcher</h1>
    <p class="repo" id="repo"></p>
    <section class="cards" id="cards" aria-live="polite"></section>
    <section class="log-panel">
      <h2>Output</h2>
      <pre id="log" aria-live="polite"></pre>
    </section>
  </main>
  <script>
    const pillClass = (status) => {{
      if (status === "Running") return "running";
      if (status === "Starting…") return "starting";
      return "stopped";
    }};

    function escapeHtml(t) {{
      return String(t).replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;");
    }}
    function escapeAttr(t) {{ return escapeHtml(t).replace(/"/g, "&quot;"); }}

    function renderCards(data) {{
      const el = document.getElementById("cards");
      el.innerHTML = data.services.map((s) => {{
        const apkNote = s.key === "mobile" && s.apk_path
          ? `<div class="sub">APK: <code>${{escapeHtml(s.apk_path)}}</code></div>`
          : "";
        const apkBuilding = s.key === "mobile" && s.apk_building;
        const exportBtn = s.key === "mobile"
          ? `<button type="button" id="export-apk-btn" ${{apkBuilding ? "disabled" : ""}} onclick="exportApk()">${{apkBuilding ? "Building APK…" : "Export APK (hosted)"}}</button>`
          : "";
        return `
        <article class="card">
          <div class="card-head">
            <div>
              <h3>${{escapeHtml(s.title)}}</h3>
              <div class="sub">${{escapeHtml(s.subtitle)}}</div>
              ${{s.url ? `<a class="link" href="${{escapeAttr(s.url)}}" target="_blank" rel="noopener">${{escapeHtml(s.url)}}</a>` : ""}}
              ${{s.key === "mobile" && s.hosted_api_url
                ? `<div class="sub">Release APK API: ${{escapeHtml(s.hosted_api_url)}}</div>` : ""}}
              ${{apkNote}}
            </div>
            <span class="pill ${{pillClass(s.status)}}">${{escapeHtml(s.status)}}</span>
          </div>
          <div class="actions">
            <button type="button" onclick="action('${{s.key}}', 'start')">Start</button>
            <button type="button" onclick="action('${{s.key}}', 'restart')">Restart</button>
            <button type="button" onclick="action('${{s.key}}', 'stop')">Stop</button>
            ${{exportBtn}}
          </div>
        </article>`;
      }}).join("");
      document.getElementById("repo").textContent = data.repo;
    }}

    async function exportApk() {{
      const buttons = document.querySelectorAll("button");
      buttons.forEach((b) => (b.disabled = true));
      try {{
        await fetch("/api/export-apk/mobile", {{ method: "POST" }});
        await refresh();
        await refreshLog();
      }} finally {{
        buttons.forEach((b) => (b.disabled = false));
      }}
    }}

    async function action(key, op) {{
      const buttons = document.querySelectorAll("button");
      buttons.forEach((b) => (b.disabled = true));
      try {{
        await fetch("/api/" + op + "/" + key, {{ method: "POST" }});
        await refresh();
        await refreshLog();
      }} finally {{
        buttons.forEach((b) => (b.disabled = false));
      }}
    }}

    async function refresh() {{
      const res = await fetch("/api/status");
      renderCards(await res.json());
    }}

    async function refreshLog() {{
      const res = await fetch("/api/logs");
      const data = await res.json();
      const pre = document.getElementById("log");
      const atBottom = pre.scrollHeight - pre.clientHeight - pre.scrollTop < 40;
      pre.textContent = data.text;
      if (atBottom) pre.scrollTop = pre.scrollHeight;
    }}

    refresh();
    refreshLog();
    setInterval(refresh, {poll_ms});
    setInterval(refreshLog, 1500);
  </script>
</body>
</html>
"""


class Handler(BaseHTTPRequestHandler):
    def log_message(self, fmt: str, *args) -> None:  # quieter console
        return

    def _json(self, code: int, payload: object) -> None:
        body = json.dumps(payload).encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self) -> None:
        path = self.path.split("?", 1)[0]
        if path in ("/", "/index.html"):
            html = index_html().encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.send_header("Content-Length", str(len(html)))
            self.end_headers()
            self.wfile.write(html)
            return
        if path == "/api/status":
            self._json(200, LAUNCHER.all_status())
            return
        if path == "/api/logs":
            with _LOG_LOCK:
                text = "".join(_LOG_LINES)
            self._json(200, {"text": text})
            return
        self.send_error(404)

    def do_POST(self) -> None:
        path = self.path.split("?", 1)[0]
        parts = path.strip("/").split("/")
        if len(parts) == 3 and parts[0] == "api" and parts[1] == "export-apk" and parts[2] == "mobile":
            LAUNCHER.build_apk_hosted()
            self._json(200, {"ok": True})
            return
        if len(parts) == 3 and parts[0] == "api" and parts[1] in ("start", "stop", "restart"):
            op, key = parts[1], parts[2]
            if key not in LAUNCHER._specs:
                self.send_error(404)
                return
            if op == "start":
                LAUNCHER.start(key)
            elif op == "stop":
                LAUNCHER.stop(key)
            else:
                LAUNCHER.restart(key)
            self._json(200, {"ok": True})
            return
        self.send_error(404)


def main() -> None:
    if not (REPO_ROOT / "AutoPro.sln").is_file():
        print(f"Expected AutoPro.sln under {REPO_ROOT}", file=sys.stderr)
        raise SystemExit(1)

    for warning in prefetch_tools():
        _log(warning + "\n")
        print(warning, file=sys.stderr)

    host = "127.0.0.1"
    url = f"http://{host}:{LAUNCHER_PORT}/"
    server = ThreadingHTTPServer((host, LAUNCHER_PORT), Handler)

    def serve() -> None:
        server.serve_forever()

    threading.Thread(target=serve, daemon=True).start()
    _log("Dev launcher ready. Open " + url + "\n")

    print(f"AutoPro Dev Launcher: {url}")
    print("Press Ctrl+C to quit (stops the launcher only, not your apps).")

    webbrowser.open(url)
    try:
        while True:
            import time

            time.sleep(3600)
    except KeyboardInterrupt:
        print("\nShutting down launcher.")
        server.shutdown()


if __name__ == "__main__":
    main()
