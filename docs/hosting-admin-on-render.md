# Host the Angular admin on Render (step by step)

This guide deploys the **Administrative Officer** desk (`web/autopro_admin`) as a **free Render static site**, talking to the same hosted API as the Flutter app.

| Piece | Render type | URL pattern |
|-------|-------------|-------------|
| API | Web service (Docker) | `https://<api-name>.onrender.com` |
| Admin UI | **Static site** | `https://<admin-name>.onrender.com` |

The static site does **not** sleep. The API on the free tier **does** sleep after ~15 minutes idle (first request after that can take ~1 minute).

---

## Before you start

1. **GitHub** — this repo is pushed to a repo you control.
2. **Neon + API on Render** — follow [Free demo hosting](../README.md#free-demo-hosting-0--month) sections 1–2 if the API is not live yet.
3. **Node.js LTS** locally (only if you want to verify the build yourself).

Demo login for the desk: **`ao.demo`** / **`ChangeMe123!`**

---

## Path A — New Blueprint (API + admin together)

Use this when you have **not** deployed from `render.yaml` yet, or you are fine creating both services from the blueprint.

### Step 1 — Push the latest repo

```bash
git add .
git commit -m "Add Render static site for Angular admin"
git push origin main
```

(`main` must match the branch you connect in Render.)

### Step 2 — Create the Blueprint on Render

1. Open [https://dashboard.render.com](https://dashboard.render.com) and sign in.
2. Click **New** → **Blueprint**.
3. Connect your **GitHub** account and select the **AutoPro** repository.
4. Render reads root **`render.yaml`**. You should see **two** services:
   - `autopro-api` (Docker web)
   - `autopro-admin` (static site)
5. When prompted, set **`ConnectionStrings__DefaultConnection`** to your **Neon** connection string (append `;Ssl Mode=Require` if Neon did not include SSL).
6. Click **Apply** / **Create**.

### Step 3 — Set the API URL for the admin build (if yours is not the default)

The admin build bakes in the API URL at **build time**.

- Default in blueprint: `https://autopro-api-qciu.onrender.com`
- If your API service has a **different** hostname:
  1. Render → **autopro-admin** → **Environment**
  2. Edit **`API_BASE_URL`** → `https://<your-api-service>.onrender.com`
  3. **Manual Deploy** → **Clear build cache & deploy** (so the bundle is rebuilt)

### Step 4 — Wait for deploys

1. **autopro-api** — status **Live** (check `/swagger`).
2. **autopro-admin** — status **Live** (open the `onrender.com` URL Render assigns).

### Step 5 — Smoke test

1. Open the admin URL (e.g. `https://autopro-admin.onrender.com`).
2. Sign in: `ao.demo` / `ChangeMe123!`
3. Open **Jobs**, **Vehicles**, **Inventory** — data should load.
4. Refresh on a deep link (e.g. `/vehicles`) — should **not** 404 (SPA rewrite).
5. If the API was sleeping, the **first** API call may be slow; retry after ~1 minute.

---

## Path B — You already have the API on Render

Use this when **`autopro-api` already exists** and you only need to add the admin.

### Option B1 — Resync Blueprint (recommended)

1. Push the updated `render.yaml` (includes `autopro-admin`).
2. Render → **Blueprints** → your AutoPro blueprint → **Manual sync** (or **Update** from repo).
3. Confirm the new **static** service `autopro-admin` is created.
4. Set **`API_BASE_URL`** on `autopro-admin` if needed (see Path A, step 3).

### Option B2 — Create static site manually

1. **New** → **Static Site** → connect the same repo.
2. **Settings:**

   | Field | Value |
   |-------|--------|
   | **Name** | `autopro-admin` (or your choice) |
   | **Branch** | `main` |
   | **Root Directory** | `web/autopro_admin` |
   | **Build Command** | `npm ci && npm run build:render` |
   | **Publish Directory** | `dist/autopro-admin/browser` |

3. **Environment** → add **`API_BASE_URL`** = `https://<your-api>.onrender.com`
4. **Redirects / Rewrites** (Render UI → **Redirects/Rewrites**):

   | Source | Destination | Action |
   |--------|-------------|--------|
   | `/*` | `/index.html` | **Rewrite** |

5. **Create Static Site** and wait until **Live**.

---

## After every code change

Push to the branch Render watches → **autopro-admin** rebuilds automatically.

To change only the API URL: update **`API_BASE_URL`** on the static site and **Clear build cache & deploy**.

---

## Verify build locally (optional)

```bash
cd web/autopro_admin
npm ci
API_BASE_URL=https://autopro-api-qciu.onrender.com npm run build:render
```

Open the built files with any static server, or use `npx serve dist/autopro-admin/browser` and test login against the hosted API.

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---------|----------------|-----|
| Refresh on `/jobs` → **404** | Missing SPA rewrite | Add `/*` → `/index.html` rewrite (blueprint `routes` or dashboard). |
| Login fails / network error | Wrong `API_BASE_URL` in bundle | Fix env var on static site, **clear cache & redeploy**. |
| Slow or timeout on first action | API cold start | Wait ~1 min; wake API via `/swagger` first. |
| CORS errors | Rare with this repo | API allows any origin in `Program.cs`; ensure admin uses **HTTPS** API URL, not `http://localhost`. |
| Build fails on Render | Node version | Render uses a supported Node LTS; run `npm ci && npm run build:render` locally to reproduce. |

---

## Custom domain (optional)

Render → **autopro-admin** → **Settings** → **Custom Domains** → follow Render’s DNS instructions. No code change required.

---

## Cost

| Service | Free tier |
|---------|-----------|
| `autopro-admin` (static) | $0 — CDN, no sleep |
| `autopro-api` (Docker) | $0 — sleeps when idle |
| Neon Postgres | $0 — see Neon limits |

---

## Files in this repo (reference)

- `render.yaml` — `autopro-admin` static service + SPA rewrite
- `web/autopro_admin/scripts/write-environment-production.mjs` — reads `API_BASE_URL` at build time
- `web/autopro_admin/src/environments/environment.production.ts` — default production API URL (overwritten during `build:render`)
