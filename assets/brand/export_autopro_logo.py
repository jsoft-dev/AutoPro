#!/usr/bin/env python3
"""Strip baked-in checkerboard and export HD transparent PNG + SVG variants."""

from __future__ import annotations

import base64
import io
from collections import deque
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parent
SOURCE = ROOT / "source.jpg"

# Logical 1x size after crop (updated each run).
LOGICAL_W = 0
LOGICAL_H = 0


def neutral_v(px: np.ndarray) -> int | None:
    r, g, b = px
    if abs(r - g) > 10 or abs(g - b) > 10:
        return None
    return int((r + g + b) // 3)


def build_alpha(arr: np.ndarray) -> np.ndarray:
    h, w = arr.shape[:2]
    transparent = np.zeros((h, w), dtype=bool)

    def is_bg(px: np.ndarray) -> bool:
        v = neutral_v(px)
        return v is not None and 170 <= v <= 238

    visited = np.zeros((h, w), dtype=bool)
    q: deque[tuple[int, int]] = deque()
    for x in range(w):
        q.extend([(0, x), (h - 1, x)])
    for y in range(h):
        q.extend([(y, 0), (y, w - 1)])

    while q:
        y, x = q.popleft()
        if visited[y, x]:
            continue
        visited[y, x] = True
        if not is_bg(arr[y, x]):
            continue
        transparent[y, x] = True
        for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < h and 0 <= nx < w and not visited[ny, nx]:
                q.append((ny, nx))

    def is_inner_checker(y: int, x: int) -> bool:
        v = neutral_v(arr[y, x])
        if v is None or not (175 <= v <= 235):
            return False
        vals: list[int] = []
        for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            ny, nx = y + dy, x + dx
            nv = neutral_v(arr[ny, nx])
            if nv is not None and 175 <= nv <= 235:
                vals.append(nv)
        return len(vals) >= 3 and max(vals) - min(vals) >= 18

    for y in range(1, h - 1):
        for x in range(1, w - 1):
            if not transparent[y, x] and is_inner_checker(y, x):
                transparent[y, x] = True

    return np.where(transparent, 0, 255).astype(np.uint8)


def pad_rgba(im: Image.Image, pixels: int = 4) -> Image.Image:
    out = Image.new("RGBA", (im.width + pixels * 2, im.height + pixels * 2), (0, 0, 0, 0))
    out.paste(im, (pixels, pixels))
    return out


def upscale_rgba(im: Image.Image, scale: int) -> Image.Image:
    """Upscale color with edge-preserving filters; alpha with Lanczos."""
    if scale == 1:
        return im

    rgb = np.array(im.convert("RGB"))
    alpha = np.array(im.split()[-1])

    bgr = cv2.cvtColor(rgb, cv2.COLOR_RGB2BGR)
    bgr = cv2.resize(bgr, None, fx=scale, fy=scale, interpolation=cv2.INTER_CUBIC)
    bgr = cv2.detailEnhance(bgr, sigma_s=14, sigma_r=0.06)
    bgr = cv2.edgePreservingFilter(bgr, flags=cv2.RECURS_FILTER, sigma_s=36, sigma_r=0.28)
    rgb_up = cv2.cvtColor(bgr, cv2.COLOR_BGR2RGB)

    alpha_up = cv2.resize(alpha, None, fx=scale, fy=scale, interpolation=cv2.INTER_LANCZOS4)

    merged = np.dstack([rgb_up, alpha_up])
    out = Image.fromarray(merged)
    return out.filter(ImageFilter.UnsharpMask(radius=1.1, percent=125, threshold=2))


def save_png(im: Image.Image, path: Path) -> None:
    im.save(path, format="PNG", compress_level=1, optimize=False)


def write_svg_linked(path: Path, png_name: str, logical_w: int, logical_h: int) -> None:
    path.write_text(
        f"""<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" width="{logical_w}" height="{logical_h}"
     viewBox="0 0 {logical_w} {logical_h}" role="img" aria-label="AutoPro by Jsoft">
  <title>AutoPro by Jsoft</title>
  <image href="{png_name}" width="{logical_w}" height="{logical_h}" preserveAspectRatio="xMidYMid meet"/>
</svg>
""",
        encoding="utf-8",
    )


def write_svg_embedded(path: Path, im: Image.Image, logical_w: int, logical_h: int) -> None:
    buf = io.BytesIO()
    im.save(buf, format="PNG", compress_level=1, optimize=False)
    b64 = base64.b64encode(buf.getvalue()).decode("ascii")
    path.write_text(
        f"""<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink"
     width="{logical_w}" height="{logical_h}" viewBox="0 0 {logical_w} {logical_h}"
     role="img" aria-label="AutoPro by Jsoft">
  <title>AutoPro by Jsoft</title>
  <image width="{logical_w}" height="{logical_h}" preserveAspectRatio="xMidYMid meet"
         xlink:href="data:image/png;base64,{b64}"/>
</svg>
""",
        encoding="utf-8",
    )


def extract_base_rgba(src: Path) -> Image.Image:
    im = Image.open(src).convert("RGB")
    arr = np.array(im, dtype=np.int16)
    alpha = build_alpha(arr)
    rgba = np.dstack([arr.astype(np.uint8), alpha])
    out = Image.fromarray(rgba)
    if bbox := out.getbbox():
        out = out.crop(bbox)
    return pad_rgba(out, pixels=4)


def main() -> None:
    global LOGICAL_W, LOGICAL_H

    base = extract_base_rgba(SOURCE)
    LOGICAL_W, LOGICAL_H = base.width, base.height

    variants = {
        1: base,
        2: upscale_rgba(base, 2),
        4: upscale_rgba(base, 4),
    }

    for scale, image in variants.items():
        suffix = "" if scale == 1 else f"@{scale}x"
        save_png(image, ROOT / f"autopro-logo-transparent{suffix}.png")

    # Default alias points at native 1x lossless export.
    save_png(variants[1], ROOT / "autopro-logo-transparent.png")

    write_svg_linked(ROOT / "autopro-logo.svg", "autopro-logo-transparent@2x.png", LOGICAL_W, LOGICAL_H)
    write_svg_linked(
        ROOT / "autopro-logo-hd.svg",
        "autopro-logo-transparent@4x.png",
        LOGICAL_W,
        LOGICAL_H,
    )
    write_svg_embedded(ROOT / "autopro-logo.embedded.svg", variants[2], LOGICAL_W, LOGICAL_H)
    write_svg_embedded(ROOT / "autopro-logo-hd.embedded.svg", variants[4], LOGICAL_W, LOGICAL_H)

    raw_master = Image.open(SOURCE)
    save_png(raw_master.convert("RGB"), ROOT / "source-master-lossless.png")

    print("Exports (logical {}×{}):".format(LOGICAL_W, LOGICAL_H))
    for scale in (1, 2, 4):
        p = ROOT / f"autopro-logo-transparent{'' if scale == 1 else f'@{scale}x'}.png"
        print(f"  {p.name}: {variants[scale].width}×{variants[scale].height}")


if __name__ == "__main__":
    main()
