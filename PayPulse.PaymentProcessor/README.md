# PayPulse.PaymentProcessor

PaymentProcessor is a small downstream service that simulates bank authorization for the sample payment flow. It does not contact a real bank or persist transactions.

## Run locally

From the repository root:

```powershell
dotnet run --project PayPulse.PaymentProcessor --launch-profile https
```

The HTTPS launch profile listens on `https://localhost:7035`.

## Endpoints

### `POST /api/payments/process`

Request:

```json
{
  "paymentId": 12345,
  "amount": 150
}
```

The amount must be greater than zero. The demo rule declines payment IDs divisible by 10 and approves other IDs. The response includes the payment ID, amount, status, and the machine name handling the request.

### `GET /health`

Returns HTTP 200 with `Healthy` when the service is responding.

## Try a request

```powershell
Invoke-RestMethod -Method Post `
  -Uri https://localhost:7035/api/payments/process `
  -ContentType 'application/json' `
  -Body '{"paymentId":12345,"amount":150}'
```

The `PaymentProcessor.http` file contains a request example.
