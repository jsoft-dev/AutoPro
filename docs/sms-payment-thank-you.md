# Payment thank-you SMS (SMSGo.lk)

When an administrative officer **receives payment and closes** a job (`POST /api/v1/admin/jobs/{id}/close`), the API can text the customer if they have a **phone** on file (required when registering a customer in the CR app).

Closing the job **always succeeds** even if SMS fails (errors are logged).

SMS uses [SMSGo.lk](https://smsgo.lk/developers/api) (~LKR 0.54/SMS, pay-as-you-go credits). Unicode (Sinhala/Tamil) is supported; the default thank-you message is English.

For **customer registration** welcome SMS, see [sms-customer-registration.md](sms-customer-registration.md). Use a **sandbox** API key (`sg_test_...`) for zero-cost HTTP tests — [SMSGo sandbox docs](https://smsgo.lk/developers/api#playground).

## No signup — log mode (default in Development)

`appsettings.Development.json` enables **`Sms:Enabled: true`** with **`Provider: Log`**.

Run the API locally, close a paid job, and read the console — you’ll see the full destination number and body. No SMSGo account required.

## Real SMS via SMSGo

1. Create an account at [SMSGo.lk](https://smsgo.lk), top up credits, and copy your API key (`sg_…`).
2. Register an **approved sender mask** in the dashboard (or omit `Mask` to use your first approved mask).
3. Store secrets with **user secrets** (do not commit the API key):

```bash
cd src/AutoPro.Api
dotnet user-secrets set "Sms:Enabled" "true"
dotnet user-secrets set "Sms:Provider" "SmsGo"
dotnet user-secrets set "Sms:ApiKey" "sg_live_YOUR_KEY"
dotnet user-secrets set "Sms:Mask" "AUTOPRO"
```

Phone numbers are normalized to Sri Lankan format (`94771234567`) from values like `0771234567` or `+94771234567`.

### Delivery webhooks (optional)

In SMSGo **Settings → Integration**, set your webhook URL to:

`https://<your-api-host>/api/v1/webhooks/smsgo`

The API logs payloads and verifies `X-SMSGo-Signature` (HMAC-SHA256 with your API key) when `Sms:Provider` is `SmsGo` and `Sms:ApiKey` is configured.

## Message content

Single SMS (Unicode-capable):

`Thank you {name}! We received your payment of Rs. {amount} for vehicle {plate} (job {job number}). We appreciate your business — please visit AutoPro again!`

## Test flow

1. Register a customer in the CR app with **your mobile number**.
2. Complete the job → WebApp: **Payment received — close job**.
3. Check your phone (SmsGo) or API logs (Log).

## Production

Set `Sms:Enabled` / `Sms:Provider` / `Sms:ApiKey` via environment variables on Render (or your host). Keep the API key in secrets, never in git.