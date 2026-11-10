# AutoPro

Auto service station management — **phase 1**: Customer Representative (Flutter) + .NET API + PostgreSQL.

## Decisions locked in

- QR code is **per vehicle** (`autopro://v/{token}`)
- Job statuses: `OnGoing`, `OnHold`, `Cancelled`, `Completed`, `Closed` (Admin **Close** → phase 2)
- Database: **PostgreSQL**

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- **PostgreSQL** (local install or Docker — see below)
- [Flutter SDK](https://docs.flutter.dev/get-started/install) (for mobile)

## Quick start

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

**Demo login:** `cr.demo` / `ChangeMe123!`

On first run, EF migrations apply and the demo CR user is seeded.

### 3. Flutter (Customer Representative)

```powershell
cd C:\Users\Janitha\Projects\AutoPro\mobile\autopro_cr
flutter create . --project-name autopro_cr
flutter pub get
flutter run
```

`flutter create .` adds Android/iOS platform folders if this repo was scaffolded without them.

**API URL:** edit `lib/core/api_config.dart` or pass:

```powershell
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5080
```

| Device | Typical `API_BASE_URL` |
|--------|-------------------------|
| Android emulator | `http://10.0.2.2:5080` |
| Windows / iOS simulator | `http://localhost:5080` |
| Physical phone | `http://<your-pc-lan-ip>:5080` |

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

## Free demo hosting ($0 / month)

Hosted demo stack that stays free: **Neon** (Postgres, no credit card, no time limit) + **Render** (free Docker web service). Avoid Render’s free Postgres — it expires after about 30 days.

| Cost | What |
|------|------|
| **$0** | Neon DB + Render free web |
| **$0** | No card required for Neon; Render free tier usually works without a card (they may ask for verification in some regions) |
| **Catch** | Render free service sleeps after 15 min idle (~1 min wake-up; first request after sleep can feel slow) |

### 1. Database (Neon)

1. Sign up at [neon.tech](https://neon.tech) → create project `autopro`.
2. Copy the connection string and append `;Ssl Mode=Require` if it is not already there.

### 2. API (Render)

1. Push this repo to GitHub.
2. [Render](https://render.com) → **New** → **Blueprint** → connect the repo (`render.yaml` is included).
3. When prompted, set `ConnectionStrings__DefaultConnection` to the Neon URL.
4. After deploy: `https://<your-service>.onrender.com/swagger` — login `cr.demo` / `ChangeMe123!`.

### 3. Flutter against the hosted API

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
```

## Next (in-repo backlog)

- Flutter: job detail, purchases, status actions (registration done in app)
- Angular admin app + `Closed` / billing (phase 2)
- CTO / Supervisor roles
