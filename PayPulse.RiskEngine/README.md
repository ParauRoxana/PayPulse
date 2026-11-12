# PayPulse.RiskEngine

RiskEngine assigns a deterministic demo risk score and runs configurable CPU work on each valid evaluation. The CPU work helps create measurable load for local and Kubernetes scaling demonstrations; it is not a fraud model.

## Run locally

From the repository root:

```powershell
dotnet run --project PayPulse.RiskEngine --launch-profile https
```

The HTTPS launch profile listens on `https://localhost:7073`.

## Endpoints

### `POST /api/risk/evaluate`

Request:

```json
{
  "paymentId": 12345,
  "amount": 150
}
```

The amount must be greater than zero. The risk score is the payment ID modulo 100; scores below 90 are approved. The response includes the payment ID, score, approval decision, machine name, measured CPU workload time, and iteration count.

### `GET /health`

Returns HTTP 200 with `Healthy` when the service is responding.

## Configure CPU work

Set `CpuWorkload:DurationMilliseconds` in app settings or use the environment variable `CpuWorkload__DurationMilliseconds`. The default is 50 ms per request; valid values are 0–2000 ms. Set it to 0 to disable CPU work.

For example, set 100 ms before starting RiskEngine in PowerShell:

```powershell
$env:CpuWorkload__DurationMilliseconds = "100"
dotnet run --project PayPulse.RiskEngine --launch-profile https
```

The `RiskEngine.http` file contains a request example.
