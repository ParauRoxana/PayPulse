# PayPulse.PaymentApi

PaymentApi is the entry point for the sample payment flow. It validates the amount, assigns a simulated payment ID, asks RiskEngine to evaluate the payment, and calls PaymentProcessor only when RiskEngine approves it.

## Run locally

Start RiskEngine and PaymentProcessor first, then run PaymentApi from the repository root:

```powershell
dotnet run --project PayPulse.RiskEngine --launch-profile https
dotnet run --project PayPulse.PaymentProcessor --launch-profile https
dotnet run --project PayPulse.PaymentApi --launch-profile https
```

PaymentApi listens on `https://localhost:7216`. Its development configuration calls RiskEngine at `https://localhost:7073` and PaymentProcessor at `https://localhost:7035`.

## Endpoints

### `POST /api/payments`

Request:

```json
{
  "amount": 150
}
```

The amount must be greater than zero. A successful HTTP response includes the generated payment ID, amount, status, risk score, and RiskEngine instance name. Approved risk evaluations also include the PaymentProcessor instance name.

Risk rejection is returned as HTTP 200 with `status: "Rejected"`. An unavailable downstream service returns 502; a downstream timeout returns 504.

### `GET /health`

Returns HTTP 200 with `Healthy` when the API process is responding.

## Try a request

```powershell
Invoke-RestMethod -Method Post `
  -Uri https://localhost:7216/api/payments `
  -ContentType 'application/json' `
  -Body '{"amount":150}'
```

The `PaymentApi.http` file contains a request example.
