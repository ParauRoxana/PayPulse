# PayPulse

PayPulse is a .NET 10 sample payment system for demonstrating Kubernetes behavior under changing traffic. It has three payment services and a traffic-control API for frontend-controlled load runs. The payment and risk decisions are simulated; no real payment provider or database is used.

## Projects

| Project | Role | Main endpoints |
| --- | --- | --- |
| [`PayPulse.PaymentApi`](PayPulse.PaymentApi/README.md) | Accepts payment requests, calls RiskEngine, then calls PaymentProcessor when risk approves. | `POST /api/payments`, `GET /health` |
| [`PayPulse.RiskEngine`](PayPulse.RiskEngine/README.md) | Assigns a deterministic demo risk score and runs configurable CPU work on each valid evaluation. | `POST /api/risk/evaluate`, `GET /health` |
| [`PayPulse.PaymentProcessor`](PayPulse.PaymentProcessor/README.md) | Simulates the bank authorization decision. | `POST /api/payments/process`, `GET /health` |
| [`PayPulse.TrafficApi`](PayPulse.TrafficApi/README.md) | Starts, stops, and reports a Black Friday load run for a frontend. | `POST /api/traffic-runs`, `GET /api/traffic-runs/{runId}`, `POST /api/traffic-runs/{runId}/stop` |

The risk score is the payment ID modulo 100; scores below 90 are approved. The processor approves IDs that are not divisible by 10. These rules exist only to make outcomes repeatable and visible in a demo.

## Run the payment flow locally

Start each payment service in a separate PowerShell terminal from the repository root, using its `https` launch profile. TrafficApi remains on its `http` profile:

```powershell
dotnet run --project PayPulse.RiskEngine --launch-profile https
dotnet run --project PayPulse.PaymentProcessor --launch-profile https
dotnet run --project PayPulse.PaymentApi --launch-profile https
dotnet run --project PayPulse.TrafficApi --launch-profile http
```

The launch profiles use these ports:

| Service | Local address |
| --- | --- |
| RiskEngine | `https://localhost:7073` |
| PaymentProcessor | `https://localhost:7035` |
| PaymentApi | `https://localhost:7216` |
| TrafficApi | `http://localhost:5090` |

PaymentApi's development configuration points to the local HTTPS addresses for RiskEngine and PaymentProcessor. TrafficApi's local configuration targets PaymentApi over HTTPS. Once the services are running, send the request in `PayPulse.PaymentApi/PaymentApi.http`:

```http
POST https://localhost:7216/api/payments
Content-Type: application/json

{
  "amount": 150
}
```

The `.http` files in each service project include requests for its main endpoint; the generated `PayPulse.*.http` files also include health checks.

## Configure RiskEngine CPU work

RiskEngine runs a CPU-bound task for each valid risk evaluation. Configure `CpuWorkload:DurationMilliseconds` in app settings or set the environment variable `CpuWorkload__DurationMilliseconds`. Values range from `0` (disabled) through `2000` milliseconds per request. The default is `50` milliseconds.

For example, to use a 100 ms workload in a PowerShell terminal before starting RiskEngine:

```powershell
$env:CpuWorkload__DurationMilliseconds = "100"
dotnet run --project PayPulse.RiskEngine --launch-profile https
```

The RiskEngine response includes the measured workload time and iteration count. The environment variable can also be set on a Kubernetes Deployment to tune the CPU pressure without rebuilding the image.

## Start and stop Black Friday traffic from a frontend

Start RiskEngine, PaymentProcessor, PaymentApi, and TrafficApi in separate terminals using the commands in [TrafficApi's README](PayPulse.TrafficApi/README.md). The frontend starts a run with `POST http://localhost:5090/api/traffic-runs`, polls `GET /api/traffic-runs/{runId}`, and stops it with `POST /api/traffic-runs/{runId}/stop`. TrafficApi returns run progress, payment outcomes, errors, latency percentiles, and request counts per RiskEngine instance. See the TrafficApi README for the request schema, CORS settings, bounds, and response examples.

## Build

Build the solution from the repository root:

```powershell
dotnet build PayPulse.slnx
```

## Deploy on Kubernetes

Local Rancher Desktop manifests, image build commands, CPU autoscaling, and the frontend traffic test are documented in [k8s/README.md](k8s/README.md). Apply the deployment with `kubectl apply -k k8s`.
