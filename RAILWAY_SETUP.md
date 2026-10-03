# Railway deployment setup

This branch prepares the current website for PostgreSQL and Linux containers. No live Azure resources or data have been moved. New PostgreSQL databases are empty; fake merchants are not seeded.

## Separate environments

- Production: GitHub `main`, ASPNETCORE_ENVIRONMENT=Production, its own PostgreSQL service and data volume, Stripe live credentials only after payment testing.
- Development: GitHub `develop`, ASPNETCORE_ENVIRONMENT=Staging, its own PostgreSQL service and volume, Stripe test credentials only. Staging retains production error handling. Do not expose developer exception pages publicly.
- Feature changes: `codex/*` branches and pull requests; require the build check before releasing to main.
- Do not share database URLs, bootstrap passwords, volumes or Stripe webhook secrets between environments.

## Railway services

For each environment, provision an app and a PostgreSQL service using the supported template. Do not expose the database TCP proxy publicly. Mount an app volume at `/data` and database storage at the template's data directory. Use one app replica while file-backed pilot request storage remains in use.

Configure app variables in Railway, not GitHub source:

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_HTTP_PORTS=8080
ReverseProxy__Enabled=true
ConnectionStrings__DefaultConnection=Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}}
PilotDataDir=/data
AppSettingsDir=/data
UploadsDir=/data/uploads
```

Use Staging for development. PostgreSQL service reference names must match the actual service name. The reverse proxy option trusts the Railway edge's forwarded HTTPS scheme; enable it only behind that edge, not on a directly exposed standalone server. The app listens on 8080. Use `/health` for the deployment healthcheck. Startup applies PostgreSQL migrations and stops if database initialization fails.

To create the first administrator, set BootstrapAdmin__Email and BootstrapAdmin__Password securely in Railway (password at least 12 characters). Remove the bootstrap password after successful account creation. It never resets an existing account or promotes an already registered public account. Existing Azure accounts are not copied automatically.

## Still required before cutover

1. Confirm the Railway plan and allowed monthly spend. The account returned “Your trial has expired. Please select a plan to continue using Railway” on project creation; no project was created.
2. Verify Linux container build, PostgreSQL migration application, real database writes/date handling, admin login and durable files on restart. GitHub smoke checks cover initialization and public pages; these alone do not establish end-to-end payment functionality.
3. Back up and inventory Azure SQL, pilot files, runtime settings and delivery photos. Decide which real records to import; do not delete or overwrite Azure data.
4. Configure Stripe, verified transactional email and environment-specific callback/webhook settings. Test real payment flows in Stripe test mode before enabling live keys.
5. Configure backup retention and verify restoring both PostgreSQL and app-volume data. A storage volume is not a backup.
6. Add tippsend.ie only after production is healthy, then configure DNS and HTTPS. Keep the existing Azure site during verification.

Production deployment, plan upgrades, domain changes, payment activation and data migration have not been performed by this branch.
