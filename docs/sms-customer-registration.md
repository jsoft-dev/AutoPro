# Customer registration SMS (SMSGo.lk)

When a customer representative **creates a new customer** (`POST /api/v1/customers`), the API can send a welcome text if the customer has a **valid Sri Lankan mobile** on file.

Creating the customer **always succeeds** even if SMS fails (errors are logged).

SMS uses [SMSGo.lk](https://smsgo.lk/developers/api). The same `Sms` configuration also drives [payment thank-you SMS](sms-payment-thank-you.md).

## No signup — log mode (default in Development)

`appsettings.Development.json` enables **`Sms:Enabled: true`** with **`Provider: Log`**.

Register a customer in the CR app and read the API console for the destination number and body. No SMSGo account required.

## Real or sandbox SMS via SMSGo

1. Create an account at [SMSGo.lk](https://smsgo.lk) and copy your API key from developer settings.
2. For **zero-cost integration tests**, use your **sandbox** key (`sg_test_...`) — same REST URL as production; balance is not deducted and delivery is simulated. See [SMSGo sandbox](https://smsgo.lk/developers/api#playground) (test numbers e.g. `94770000000`).
3. For **live** texts, use `sg_live_...` and an approved **mask**.
4. **Local dev:** use **user secrets** for the API key (do not commit it). Defaults for `Mask` / `BaseUrl` live in `appsettings.Development.json`:

```bash
cd src/AutoPro.Api
dotnet user-secrets set "Sms:Enabled" "true"
dotnet user-secrets set "Sms:Provider" "SmsGo"
dotnet user-secrets set "Sms:ApiKey" "sg_test_YOUR_SANDBOX_KEY"
dotnet user-secrets set "Sms:Mask" "SMSGO"
```

Phone numbers are normalized to Sri Lankan format (`94771234567`) from values like `0771234567` or `+94771234567`.

**Note:** Registration SMS runs only when a **new** customer is created, not when the CR app reuses an existing customer during vehicle registration.

## Message content

`Welcome {name}! You are registered with AutoPro. We will send service updates to this number.`

## Test flow

1. **Log:** Run API in Development, register a new customer in the CR app → console shows welcome SMS.
2. **Sandbox:** Set `sg_test_...`, use phone `0770000000` (maps to `94770000000`) → verify `SmsGoSmsSender` success logs (no handset delivery in sandbox).
3. **Live:** Switch to `sg_live_...` and your real mobile on a new customer.

## Production

Set `Sms:Enabled` / `Sms:Provider` / `Sms:ApiKey` via environment variables on your host. Use `sg_live_...` in production. Keep API keys in secrets, never in git.