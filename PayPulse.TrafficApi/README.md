# PayPulse.TrafficApi

TrafficApi is a backend control service for starting, stopping, and monitoring a bounded Black Friday traffic run. It sends payment requests to the configured PaymentApi address. A frontend calls these endpoints; it does not generate requests itself.

## Run locally

Start RiskEngine, PaymentProcessor, and PaymentApi first. Then start TrafficApi from the repository root:

```powershell
dotnet run --project PayPulse.RiskEngine --launch-profile https
dotnet run --project PayPulse.PaymentProcessor --launch-profile https
dotnet run --project PayPulse.PaymentApi --launch-profile https
dotnet run --project PayPulse.TrafficApi --launch-profile http
```

TrafficApi listens on `http://localhost:5090` and targets PaymentApi at `https://localhost:7216` by default. Set `Traffic__PaymentApiBaseUrl` to point at a different PaymentApi address, such as a Kubernetes Service URL.

By default, browser requests are allowed from `http://localhost:5173`, `http://localhost:3000`, and `http://localhost:4200`. Configure `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, and so on for the frontend's actual origin.

## Endpoints

### Start a run: `POST /api/traffic-runs`

Request body (all fields are optional):

```json
{
  "durationSeconds": 60,
  "requestsPerSecond": 20,
  "concurrency": 20,
  "amount": 150
}
```

Returns HTTP 202 and a run snapshot containing `runId`, `status`, progress, the steady request rate, and current statistics. A second run cannot start while one is active; that request returns HTTP 409 with the active run snapshot.

Bounds: duration 1–120 seconds, rate 1–200 requests/sec, concurrency 1–100, and amount greater than 0 through 1,000,000. The request rate stays constant for the whole run. The target is fixed by `Traffic:PaymentApiBaseUrl`; the request cannot supply an arbitrary target URL.

### Get the current active run: `GET /api/traffic-runs/active`

Returns HTTP 200 with a live snapshot, or HTTP 204 when no run is active.

### Get run status: `GET /api/traffic-runs/{runId}`

Returns HTTP 200 with the run snapshot. The snapshot includes scheduled requests, responses, payment outcomes, errors, latency percentiles, and request counts by RiskEngine instance. The latest run remains available until another run starts or TrafficApi restarts.

### Stop a run: `POST /api/traffic-runs/{runId}/stop`

Cancels new scheduling and in-flight requests, then returns the run snapshot. Poll the run status until `status` is `stopped` to retrieve the final statistics. Stop is safe to call again for the latest run.

## Example

```powershell
$run = Invoke-RestMethod -Method Post `
  -Uri http://localhost:5090/api/traffic-runs `
  -ContentType 'application/json' `
  -Body '{"durationSeconds":60,"requestsPerSecond":20,"concurrency":20,"amount":150}'

Invoke-RestMethod -Method Get -Uri "http://localhost:5090/api/traffic-runs/$($run.runId)"
Invoke-RestMethod -Method Post -Uri "http://localhost:5090/api/traffic-runs/$($run.runId)/stop"
```

Run state is held in memory and TrafficApi is designed as a single control instance for this demo. Keep one TrafficApi replica while the frontend polls or stops a run. PaymentApi, RiskEngine, and PaymentProcessor can scale independently.
