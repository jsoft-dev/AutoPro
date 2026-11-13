# AutoPro

Auto service station management — **phase 1**: Customer Representative (Flutter) + **The WebApp** (Angular) + .NET API + PostgreSQL.

## Decisions locked in

- QR code is **per vehicle** (`autopro://v/{token}`)
- Job statuses: `OnGoing`, `OnHold`, `Cancelled`, `Completed`, `Closed` (Admin **Close** after payment)
- Database: **PostgreSQL**

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- **PostgreSQL** (local install or Docker — see below)
- [Flutter SDK](https://docs.flutter.dev/get-started/install) (for mobile)
- [Node.js LTS](https://nodejs.org/) + Angular CLI (`npm i -g @angular/cli`) for the WebApp

## Quick start

### Dev launcher (optional)

Opens a local web UI (in your browser) to start/stop the API, WebApp, and Flutter app:

```bash
python3 tools/dev-launcher/launcher.py
```

Then use **http://127.0.0.1:8765** — or double-click `tools/dev-launcher/AutoPro Dev Launcher.command` on macOS.

### 1. Database (local PostgreSQL)

Create the app database and user (password must match `appsettings.json`, or edit the connection string).

**Option A — pgAdmin:** open Query Tool as `postgres` and run `scripts/init-autopro-db.sql`.

**Option B — psql (PowerShell):**

```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -f "C:\Users\Janitha\Projects\AutoPro\scripts\init-autopro-db.sql"
```

If you prefer your own Postgres login, change `ConnectionStrings:DefaultConnection` in `src/AutoPro.Api/appsettings.json` (or use [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)) and create only the `autopro` database.

**Optional — Docker instead of local install:**

```powershell
cd C:\Users\Janitha\Projects\AutoPro
docker compose up -d
```

### 2. API

```powershell
cd C:\Users\Janitha\Projects\AutoPro
dotnet restore
dotnet run --project src\AutoPro.Api\AutoPro.Api.csproj
```

Swagger: http://localhost:5080/swagger

**Demo logins:** `cr.demo` / `ChangeMe123!` (customer rep), `ao.demo` / `ChangeMe123!` (WebApp)

On first run, EF migrations apply and the demo CR user is seeded.

### 3. WebApp (Angular)

```bash
cd web/autopro_admin
npm install
npm start
```

The WebApp: http://localhost:4200 — sign in as `ao.demo`.

Set the API URL in `src/environments/environment.ts` (default `http://localhost:5080`).

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/v1/jobs`, `/today`, `/{id}` | Same job views (AO + CR) |
| GET | `/api/v1/admin/vehicles`, `/{id}` | Registered vehicles + `qrPayload` for print |
| POST | `/api/v1/admin/jobs/{id}/close` | Payment received → `Closed`, printable bill; optional thank-you email |

**Vehicles** (nav): search by plate, customer, or phone → open a vehicle → **Print QR label** for the customer.

### 4. Flutter (Customer Representative)

```powershell
cd C:\Users\Janitha\Projects\AutoPro\mobile\autopro_cr
flutter create . --project-name autopro_cr
flutter pub get
flutter run
```

`flutter create .` adds Android/iOS platform folders if this repo was scaffolded without them.

**API URL:** default in `lib/core/api_config.dart` is the hosted API (`https://autopro-api-qciu.onrender.com`). Override for local dev:

```powershell
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5080
```

| Device | Typical `API_BASE_URL` |
|--------|-------------------------|
| Physical phone / release APK | `https://autopro-api-qciu.onrender.com` (default) |
| Android emulator + local API | `http://10.0.2.2:5080` |
| Windows / iOS simulator | `http://localhost:5080` |

**Android APK (real device testing):**

```powershell
cd mobile\autopro_cr
flutter pub get
flutter build apk --release
```

Release APK path: `mobile\autopro_cr\build\app\outputs\flutter-apk\app-release.apk`

Optional fat APK with explicit API (same as default):

```powershell
flutter build apk --release --dart-define=API_BASE_URL=https://autopro-api-qciu.onrender.com
```

Copy the APK to the phone and install (enable “Install unknown apps” if sideloading). First request after Render sleep may take ~1 minute.

## Phase 1 API (CR)

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/v1/auth/login` | JWT login |
| POST | `/api/v1/customers` | Register customer |
| POST | `/api/v1/customers/{id}/vehicles` | Register vehicle + QR token |
| GET | `/api/v1/vehicles/by-qr/{token}` | Resolve vehicle from scan |
| GET | `/api/v1/vehicles/by-plate?plate=` | Resolve vehicle by registration number |
| POST | `/api/v1/jobs` | Take job → `OnGoing`, nullable odometer |
| GET | `/api/v1/jobs/today` | Today’s jobs |
| GET | `/api/v1/jobs/{id}` | Job detail |
| PATCH | `/api/v1/jobs/{id}/status` | CR status changes (not `Closed`) |
| POST | `/api/v1/jobs/{id}/purchases` | Add inventory / vendor line |
| POST | `/api/v1/admin/jobs/{id}/close` | AO: payment received → `Closed` |
| GET | `/api/v1/admin/vehicles` | AO: list vehicles (`?q=` search, `?limit=` up to 200) |
| GET | `/api/v1/admin/vehicles/{id}` | AO: vehicle detail incl. `qrPayload` |

## Free demo hosting ($0 / month)

Hosted demo stack that stays free: **Neon** (Postgres, no credit card, no time limit) + **Render** (free Docker web service). Avoid Render’s free Postgres — it expires after about 30 days.

| Cost | What |
|------|------|
| **$0** | Neon DB + Render API + WebApp (Render static site) |
| **$0** | No card required for Neon; Render free tier usually works without a card (they may ask for verification in some regions) |
| **Catch** | Render **API** sleeps after 15 min idle (~1 min wake-up; first request after sleep can feel slow). The **WebApp** static site does not sleep. |

### 1. Database (Neon)

1. Sign up at [neon.tech](https://neon.tech) → create project `autopro`.
2. Copy the connection string and append `;Ssl Mode=Require` if it is not already there.

### 2. API (Render)

1. Push this repo to GitHub.
2. [Render](https://render.com) → **New** → **Blueprint** → connect the repo (`render.yaml` is included).
3. When prompted, set `ConnectionStrings__DefaultConnection` to the Neon URL.
4. After deploy: `https://<your-service>.onrender.com/swagger` — login `cr.demo` / `ChangeMe123!`.

### 3. WebApp on Render (static site)

Full walkthrough: **[docs/hosting-webapp-on-render.md](docs/hosting-webapp-on-render.md)**

Short version:

1. Push this repo (includes the WebApp as `autopro-admin` in `render.yaml`, or add the static site manually — see the doc).
2. Ensure the API is live (`https://<your-api>.onrender.com/swagger`).
3. Open the WebApp Render URL (service **`autopro-admin`**) → sign in **`ao.demo`** / **`ChangeMe123!`**
4. If your API hostname is not `autopro-api-qciu.onrender.com`, set build env **`API_BASE_URL`** on the static site and redeploy with clear build cache.

### 4. Flutter against the hosted API

```powershell
flutter run --dart-define=API_BASE_URL=https://<your-service>.onrender.com
```

Use **HTTPS** for physical devices (required on Android unless you allow cleartext).

### Strictest $0 (no cloud signup)

Run API + Postgres locally (`docker compose up -d`, `dotnet run`), expose with [Cloudflare Tunnel](https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/) — free URL while your PC is on.

## Solution layout

```
AutoPro/
  docker-compose.yml
  Dockerfile
  render.yaml
  src/
    AutoPro.Api/
    AutoPro.Application/
    AutoPro.Domain/
    AutoPro.Infrastructure/
  mobile/autopro_cr/    # Flutter CR app
  web/autopro_admin/    # The WebApp (Angular)
```

## Email (payment thank-you, optional)

After AO **close** (payment), the API can email the customer when their profile includes an email. Local dev defaults to **log-only** (no SMTP signup). See **[docs/email-payment-thank-you.md](docs/email-payment-thank-you.md)**.

## Next (in-repo backlog)

- CTO / Supervisor roles
- ~~Itemized labor charges (not only purchase lines)~~ — job-type service charge on invoice at completion; configure under **Business settings** in admin
- Persist payment method / receipt number on close
