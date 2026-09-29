# Deploying the Platform Admin app to IIS

This app is a static single-page app (React + Vite) — same deployment shape as
`StaffingManagementSystem.Web`, just a separate IIS site on its own subdomain so it's never
reachable from the tenant-facing app's origin.

Target: **https://platformadmin.itmusketeers.com**

## 1. Build

```
cd StaffingManagementSystem.PlatformWeb
npm install
npm run build
```

This produces a `dist/` folder. Vite automatically copies everything in `public/` (including
`web.config`) into `dist/`, so the SPA routing fix below ships with the build — no manual copy
step needed.

`npm run build` picks up `.env.production` automatically (already committed, pointing at
`https://zentaviocrmapi.itmusketeers.com`), so you don't need to set `VITE_API_BASE_URL`
by hand for a production build.

## 2. Copy to the server

Copy the contents of `dist/` (not the folder itself — its *contents*) to wherever this site's
physical path will be, e.g. `C:\inetpub\platformadmin.itmusketeers.com\`.

## 3. IIS site setup

1. Confirm the **URL Rewrite** module is installed (Microsoft, free) — it's what makes
   `web.config`'s SPA fallback rule work. It's almost certainly already installed since
   `StaffingManagementSystem.Web` depends on it too; if not, install it before continuing.
2. In IIS Manager, add a new website:
   - **Site name:** `platformadmin.itmusketeers.com` (or similar)
   - **Physical path:** the folder from step 2
   - **Binding:** HTTPS, host name `platformadmin.itmusketeers.com`, with a valid TLS
     certificate for that hostname (see step 5)
3. Point DNS for `platformadmin.itmusketeers.com` at the same server/IP the other
   `*.itmusketeers.com` sites resolve to.
4. No application pool changes are needed beyond IIS's defaults — this is static file hosting,
   not a .NET app, so the app pool can even be "No Managed Code."
5. Obtain/renew a TLS certificate for `platformadmin.itmusketeers.com` the same way the other
   `itmusketeers.com` subdomains are certified (e.g. via whatever ACME/Let's Encrypt or
   commercial-CA process is already in use for `zentaviocrm.itmusketeers.com`).

## 4. API-side checklist

The API already trusts this origin — `Cors:AllowedOrigins` in
`StaffingManagementSystem.api/appsettings.json` includes `https://platformadmin.itmusketeers.com`.
If that value is ever changed, **restart the API** — `Cors:AllowedOrigins` is read once at
startup, not per-request.

## 5. First login in production

Sign in with the seeded bootstrap admin (`platformadmin@zentaviocrm.com` /
`PlatformAdmin@123` — see `SQL Changes/Tenant Admin/003_SeedFirstPlatformAdmin.sql`), then
**immediately change that password** via the admin menu in the top bar (Change Password) —
that seeded password is sitting in source control and should never be the live one. Create any
additional platform admin accounts you need from the Platform Admins page, then consider
deactivating the seeded account entirely once you have a named replacement.

## Redeploying after a change

Same three steps every time: `npm run build` -> copy `dist/`'s contents over the existing
site folder -> no IIS restart needed for a static site (IIS just serves the new files
immediately).
