# PayPulse frontend

React dashboard built with Vite. It sends individual payments to PaymentApi and controls Black Friday traffic runs through TrafficApi.

## Requirements

Install these tools before starting the project:

- **Node.js 20.19 or newer in the 20.x line, or 22.12 or newer in the 22.x line.** These versions meet the engine requirement of the Vite version pinned in `package-lock.json`. Check with `node --version`.
- **npm**, included with Node.js. Check with `npm --version`.
- **.NET 10 SDK**, required to run the backend services. Check with `dotnet --version`.
- **Git**, if you need to clone the repository.

No database, payment provider, Kubernetes cluster, or global React/Vite installation is needed for local development. The payment decisions are simulated by the backend.

## Install frontend dependencies

Open PowerShell in the frontend directory:

```powershell
cd path\to\paypulse\frontend
npm ci
```

`npm ci` installs the exact versions in `package-lock.json`. Run it again after pulling changes to the lockfile. You do not need to install React or Vite globally.

## Start the complete application locally

The frontend calls both API services, and TrafficApi itself sends its generated payments to PaymentApi. Start each backend in its own PowerShell window from the repository root (`path\to\paypulse`). Keep all four windows running:

**Terminal 1 — RiskEngine**

```powershell
dotnet run --project PayPulse.RiskEngine --launch-profile https
```

**Terminal 2 — PaymentProcessor**

```powershell
dotnet run --project PayPulse.PaymentProcessor --launch-profile https
```

**Terminal 3 — PaymentApi**

```powershell
dotnet run --project PayPulse.PaymentApi --launch-profile https
```

**Terminal 4 — TrafficApi**

```powershell
dotnet run --project PayPulse.TrafficApi --launch-profile http
```

These are the default local endpoints:

| Service | Address | Used for |
| --- | --- | --- |
| RiskEngine | `https://localhost:7073` | Payment risk evaluation |
| PaymentProcessor | `https://localhost:7035` | Simulated bank authorization |
| PaymentApi | `https://localhost:7216` | Individual payment requests |
| TrafficApi | `http://localhost:5090` | Start, stop, and inspect traffic runs |

The development settings already configure PaymentApi to call RiskEngine and PaymentProcessor, and TrafficApi to call PaymentApi. No extra backend URL configuration is needed when using these launch profiles.

Once the four services have started, open a fifth PowerShell window in `frontend` and run:

```powershell
npm run dev
```

Open the local URL printed by Vite, normally `http://localhost:5173`. Leave the terminal running while you use the page.

## API routing used by Vite

During local development, the browser sends requests to Vite. Vite forwards them to the backend, so the browser does not need direct CORS access to the services:

- `/api/*` is forwarded to `https://localhost:7216` (PaymentApi).
- `/traffic-api/*` is forwarded to `http://localhost:5090`, with the `/traffic-api` prefix removed (TrafficApi).

The HTTPS proxy accepts the local development certificate. If you change backend ports, update the proxy targets in `vite.config.js` to match.

The dashboard sends individual payments to `POST /api/payments`. The **Simulate Black Friday** button starts a backend run with a duration of 60 seconds, 20 requests per second, concurrency 20, and amount 150. The dashboard polls the run for response and outcome statistics; **Stop** asks TrafficApi to cancel it. TrafficApi limits duration to 1–120 seconds, request rate to 1–200 per second, and concurrency to 1–100. See [TrafficApi's README](../PayPulse.TrafficApi/README.md) for the full run contract.

Recent individual payments are kept in this browser's `localStorage`. The backend currently has no payment history endpoint or database, so clearing browser storage removes that displayed history. Black Friday requests and their statistics are held in TrafficApi memory and are lost if that service restarts.

## Build and lint

From the `frontend` directory:

```powershell
npm run build
npm run lint
```

The production files are written to `frontend/dist`. Vite's development proxy is not included in a production build. For a deployed frontend, provide a reverse proxy that routes requests to both APIs, or set the API origins before building:

```powershell
$env:VITE_PAYMENT_API_BASE_URL = "https://payment-api.example.com"
$env:VITE_TRAFFIC_API_BASE_URL = "https://traffic-api.example.com"
npm run build
```

Use API origins without a trailing slash. `VITE_PAYMENT_API_BASE_URL` defaults to the current origin, and `VITE_TRAFFIC_API_BASE_URL` defaults to `/traffic-api`. These values are embedded in the generated frontend at build time.

## Troubleshooting

- **`vite` is not recognized:** run `npm ci` from the `frontend` directory, then retry the command.
- **Node version is unsupported:** install a Node.js version listed under [Requirements](#requirements), reopen PowerShell, and check `node --version`.
- **Payment request reports that a service is unavailable:** check that RiskEngine and PaymentProcessor started successfully before PaymentApi, and confirm the launch-profile ports above.
- **Black Friday start fails or the dashboard cannot load traffic:** check that TrafficApi is running on port `5090`. It also needs PaymentApi to be available to send the simulated payments.
- **A port is already in use:** stop the other process using it, or change that service's launch profile and update `vite.config.js` and the relevant backend service URL settings to match.

For the full backend setup, request examples, and Kubernetes instructions, see the [repository README](../README.md).
