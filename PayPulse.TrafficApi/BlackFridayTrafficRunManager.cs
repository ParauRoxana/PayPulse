internal sealed class BlackFridayTrafficRunManager(
    IHttpClientFactory httpClientFactory)
{
    private readonly object _gate = new();
    private BlackFridayTrafficRun? _latestRun;

    public BlackFridayTrafficRun? Start(StartTrafficRequest request)
    {
        lock (_gate)
        {
            if (_latestRun?.IsActive == true)
                return null;

            var run = new BlackFridayTrafficRun(
                request,
                httpClientFactory.CreateClient(BlackFridayTrafficRun.PaymentApiClientName));
            _latestRun = run;
            run.Start();
            return run;
        }
    }

    public BlackFridayTrafficRun? Get(Guid runId)
    {
        lock (_gate)
            return _latestRun?.RunId == runId ? _latestRun : null;
    }

    public TrafficRunSnapshot? GetActive()
    {
        lock (_gate)
            return _latestRun?.IsActive == true ? _latestRun.GetSnapshot() : null;
    }
}
