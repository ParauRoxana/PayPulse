# PayPulse on Kubernetes

These manifests deploy the four services into the `paypulse` namespace. They use local image tags, HTTP between services, health probes, resource requests, and a CPU-based HPA for RiskEngine. TrafficApi stays at one replica because run state is held in memory.

## 1. Start Rancher Desktop Kubernetes

Enable Kubernetes in Rancher Desktop and wait for the cluster to become ready. Then check the active context and node:

```powershell
kubectl config get-contexts
kubectl get nodes
```

## 2. Build the local images

Run these commands from the repository root. With Rancher Desktop's `containerd` engine, build into its Kubernetes image namespace:

```powershell
nerdctl --namespace k8s.io build -t paypulse-risk-engine:local -f PayPulse.RiskEngine/Dockerfile .
nerdctl --namespace k8s.io build -t paypulse-payment-processor:local -f PayPulse.PaymentProcessor/Dockerfile .
nerdctl --namespace k8s.io build -t paypulse-payment-api:local -f PayPulse.PaymentApi/Dockerfile .
nerdctl --namespace k8s.io build -t paypulse-traffic-api:local -f PayPulse.TrafficApi/Dockerfile .
```

If using the `dockerd` engine, use the Docker CLI instead, with the same tags and `-f` paths.

## 3. Deploy the stack

```powershell
kubectl apply -k k8s
kubectl -n paypulse get deployments,services,hpa
kubectl -n paypulse get pods -w
```

PaymentApi and TrafficApi receive Kubernetes Service URLs through environment variables. Inside the cluster they use HTTP on port 8080; local HTTPS addresses from `appsettings.Development.json` are not used.

## 4. Check CPU metrics

The HPA needs CPU metrics and CPU requests. The RiskEngine Deployment defines both. Check that metrics are available:

```powershell
kubectl top pods -n paypulse
kubectl get hpa -n paypulse
```

If `kubectl top` reports that the Metrics API is unavailable, enable or install a metrics provider for the cluster before testing autoscaling. Kubernetes HPA uses the CPU request to calculate utilization; without a request, it cannot calculate the target percentage.

## 5. Send traffic through TrafficApi

Forward its Kubernetes Service to the local machine:

```powershell
kubectl -n paypulse port-forward service/trafficapi 5090:80
```

In another terminal, start a 120-second run at a steady 20 requests per second:

```powershell
$run = Invoke-RestMethod -Method Post `
  -Uri http://localhost:5090/api/traffic-runs `
  -ContentType 'application/json' `
  -Body '{"durationSeconds":120,"requestsPerSecond":20,"concurrency":20,"amount":150}'

$run.runId
```

Watch the RiskEngine HPA and pods in separate terminals:

```powershell
kubectl get hpa riskengine -n paypulse -w
```

```powershell
kubectl get pods -n paypulse -l app=riskengine -w
```

Read the live run statistics, including requests processed by each RiskEngine instance:

```powershell
Invoke-RestMethod -Uri "http://localhost:5090/api/traffic-runs/$($run.runId)"
```

Stop the run when finished:

```powershell
Invoke-RestMethod -Method Post `
  -Uri "http://localhost:5090/api/traffic-runs/$($run.runId)/stop"
```

The HPA may take a little while to scale down after the run. The configured 30-second stabilization window makes the local demo settle sooner than Kubernetes' longer default.

## 6. Demonstrate self-healing

While the stack is running, get a RiskEngine pod name and delete that one pod:

```powershell
kubectl get pods -n paypulse -l app=riskengine
kubectl delete pod -n paypulse <riskengine-pod-name>
```

The Deployment creates a replacement pod. Watch it with `kubectl get pods -n paypulse -w`.

## Cleanup

```powershell
kubectl delete -k k8s
```
