# TippSend launch setup

Production website and PostgreSQL run on Railway. Development and test databases run locally or in GitHub Actions; no paid Railway development environment is required.

Railway deploys `main` after GitHub checks pass. Sleep mode is disabled by owner request. Hobby has a $5 minimum, including $5 of usage; actual usage and taxes may increase the bill. A $5 email alert is configured, not a $5 hard ceiling.

## Required private variables

- `ConnectionStrings__DefaultConnection`: private Railway PostgreSQL connection, SSL required.
- `ASPNETCORE_ENVIRONMENT=Production`, `ReverseProxy__Enabled=true`, port 8080.
- `AppSettingsDir=/data`, `PilotDataDir=/data`, `UploadsDir=/data/uploads`, persistent web volume.
- `PublicBaseUrl`: the current HTTPS website URL; switch to https://tippsend.ie after DNS and HTTPS verification.
- `Notifications__OperatorEmail`: owner's receiving address. Never commit it to source.
- `Resend__ApiKey`: sending-only key restricted to tippsend.ie; `Resend__FromAddress=orders@tippsend.ie`.
- `Stripe__SecretKey`: sandbox/test key first.
- `Stripe__WebhookSecret`: signing secret for `/webhooks/stripe`, checkout.session.completed and checkout.session.async_payment_succeeded.
- Keep `Stripe__EnableLivePayments=false` until verified test checkout, operational policy and live Stripe account onboarding are complete.

## First administrator

Use `BootstrapAdmin__Email`, a random `BootstrapAdmin__SetupToken`, and `BootstrapAdmin__SetupExpiresAt` (UTC, at most 24 hours). Owner opens `/Setup?token=...` and chooses their own password. Setup creates an admin only if no admins exist, never promotes existing public users, and is protected by a transaction lock. Remove setup variables after creation. Public registration is disabled. Enable MFA through Identity account settings. Password resets need the email provider connected.

## Booking flow

Send an Item creates a delivery request and private link. Requests, routes, runtime settings and business enquiries persist in PostgreSQL. Existing operational files are imported on first access and remain unchanged on disk. Operator confirms availability and a positive quote before payment becomes available. Paid test orders are clearly marked and excluded from revenue summaries. Requests reserve scheduled capacity until cancelled; completed deliveries retain their reservation.

Payment drafts persist before leaving for Stripe. Return and signed webhook use the same database transaction and lock. Currency, mode, test/live environment, Stripe session ID and exact amount are checked. Provider failures return a retryable error. Confirmation emails are queued in the payment transaction and retried by the worker. Card details are never stored.

## Backups

Admin > Download a backup exports a database snapshot plus persistent files. Archive contains private customer data and encryption keys: keep it securely off Railway. CI restores the SQL into a new disposable PostgreSQL database. Never restore directly into production without a separately reviewed plan and a current backup. Automatic off-site backup scheduling is still required for unattended operation; no Pro upgrade is configured.

## Domain and email

Domain registration is pending registry approval. Add exact Railway verification/CNAME and Resend records after activation. An apex CNAME requires DNS flattening/ALIAS support. Free Cloudflare DNS plus Email Routing is an option for receiving mail at hello@tippsend.ie and forwarding to the owner's existing inbox. Resend is outbound service email, not a full personal mailbox. Do not enable paid overages.

## Before accepting real deliveries

Confirm service area, availability, handling restrictions, cancellation/refund arrangements and appropriate vehicle/parcel cover. Review public terms/privacy for actual operations and hosting location. Verify DNS/HTTPS, actual email delivery, password reset, admin access, an actual Stripe sandbox checkout and signed webhook, delivery status/photo access, and a restorable off-site backup. Do not claim launch completion while these checks remain outstanding.
