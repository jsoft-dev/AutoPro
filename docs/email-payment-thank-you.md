# Payment thank-you email

When an administrative officer **receives payment and closes** a job (`POST /api/v1/admin/jobs/{id}/close`), the API can email the customer if they have an **email** on file (optional field when registering a customer in the CR app).

Closing the job **always succeeds** even if email fails (errors are logged).

## No signup — log mode (default in Development)

`appsettings.Development.json` enables **`Email:Enabled: true`** with **`Provider: Log`**.

Run the API locally, close a paid job, and read the console — you’ll see the full to/subject/body. No Gmail, Twilio, or SendGrid required.

## Real email via SMTP (e.g. Gmail you already have)

Use **user secrets** (do not commit passwords):

Run from **`src/AutoPro.Api`** (the project defines `UserSecretsId` so `dotnet user-secrets` works):

```bash
cd src/AutoPro.Api
dotnet user-secrets set "Email:Enabled" "true"
dotnet user-secrets set "Email:Provider" "Smtp"
dotnet user-secrets set "Email:FromAddress" "janithachamara1995@gmail.com"
dotnet user-secrets set "Email:FromName" "AutoPro"
dotnet user-secrets set "Email:SmtpHost" "smtp.gmail.com"
dotnet user-secrets set "Email:SmtpPort" "587"
dotnet user-secrets set "Email:SmtpUser" "janithachamara1995@gmail.com"
dotnet user-secrets set "Email:SmtpPassword" "hmfsfzbiqzwyidzj"
```

For Gmail SMTP you need an [App Password](https://myaccount.google.com/apppasswords), **not** your normal Gmail password.

1. Turn on [2-Step Verification](https://myaccount.google.com/signinoptions/two-step-verification) first.
2. Then open [App passwords](https://myaccount.google.com/apppasswords) → create one for “Mail” → paste it into `Email:SmtpPassword`.

### “This setting is not available for your account” (App passwords)

Google shows that screen when App passwords are **disallowed** for your account. Common causes:

| Cause | What to do |
|--------|------------|
| **2-Step Verification off** | Enable 2SV, wait a few minutes, try App passwords again. |
| **Work / school (Google Workspace)** | Personal `@gmail.com` usually works; Workspace needs the admin to allow app passwords (or use the org’s SMTP relay). |
| **Advanced Protection** on the account | App passwords are blocked — use another sender or stay on **Log** mode. |
| **Some managed / supervised accounts** | Use personal Gmail or skip SMTP (see below). |

**Easiest path:** keep **`Provider: Log`** (default in Development) — no Gmail setup, full message in the API console when you close a job.

**SMTP without Gmail:** use any mailbox your host gives you (e.g. your domain’s cPanel **Outgoing server**, Microsoft 365 SMTP). Same `Email:Provider: Smtp` settings; only host/user/password change.

## Message content

Subject: `Thank you for your payment — AutoPro ({job number})`

Body includes customer name, **Rs. {amount}** (bill total), plate number, and job number.

## Test flow

1. Register a customer in the CR app with **your email** in the optional email field.
2. Complete the job → WebApp: **Payment received — close job**.
3. Check inbox (Smtp) or API logs (Log).

## Production

Use your domain’s SMTP (Microsoft 365, Google Workspace, Amazon SES SMTP, etc.) and set `FromAddress` to a real `noreply@yourdomain.com`.
