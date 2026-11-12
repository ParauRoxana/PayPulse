using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

internal sealed class BlackFridayTrafficRun
{
    public const string PaymentApiClientName = "PaymentApi";

    private readonly object _gate = new();
    private readonly StartTrafficRequest _request;
    private readonly HttpClient _client;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly TrafficStatistics _statistics = new();
    private long _startedTimestamp;
    private DateTimeOffset _startedAt;
    private DateTimeOffset? _finishedAt;
    private string _status = "created";
    private string? _error;

    public BlackFridayTrafficRun(StartTrafficRequest request, HttpClient client)
    {
        _request = request;
        _client = client;
    }

    public Guid RunId { get; } = Guid.NewGuid();
    public bool IsActive
    {
        get
        {
            lock (_gate)
                return _status is "running" or "stopping";
        }
    }

    public void Start()
    {
        lock (_gate)
        {
            _startedAt = DateTimeOffset.UtcNow;
            _startedTimestamp = Stopwatch.GetTimestamp();
            _status = "running";
            _ = Task.Run(ExecuteAsync);
        }
    }

    public void RequestStop()
    {
        lock (_gate)
        {
            if (_status == "running")
            {
                _status = "stopping";
                _cancellation.Cancel();
            }
        }
    }

    public TrafficRunSnapshot GetSnapshot()
    {
        string status;
        DateTimeOffset startedAt;
        DateTimeOffset? finishedAt;
        string? error;
        lock (_gate)
        {
            status = _status;
            startedAt = _startedAt;
            finishedAt = _finishedAt;
            error = _error;
        }

        var elapsed = _startedTimestamp == 0 ? TimeSpan.Zero : Stopwatch.GetElapsedTime(_startedTimestamp);
        var progress = status == "completed"
            ? 100d
            : Math.Clamp(elapsed.TotalSeconds / _request.DurationSeconds * 100d, 0d, 100d);

        return new TrafficRunSnapshot(
            RunId,
            "black-friday",
            status,
            startedAt,
            finishedAt,
            _request.DurationSeconds,
            progress,
            _request.RequestsPerSecond,
            _request.Concurrency,
            _request.Amount,
            _statistics.GetSnapshot(elapsed),
            error);
    }

    private async Task ExecuteAsync()
    {
        try
        {
            await Parallel.ForEachAsync(
                ScheduledRequests(_cancellation.Token),
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = _request.Concurrency,
                    CancellationToken = _cancellation.Token
                },
                async (paymentRequest, token) => await SendRequestAsync(_client, paymentRequest, _statistics, token));
        }
        catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
        {
            // The stop endpoint cancels scheduling and in-flight requests.
        }
        catch (Exception exception)
        {
            lock (_gate)
                _error = exception.Message;
        }

        lock (_gate)
        {
            _finishedAt = DateTimeOffset.UtcNow;
            if (_error is not null)
            {
                _status = "failed";
            }
            else if (_cancellation.IsCancellationRequested)
            {
                _status = "stopped";
            }
            else
            {
                _status = "completed";
            }
        }
    }

    private async IAsyncEnumerable<PaymentRequest> ScheduledRequests(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (Stopwatch.GetElapsedTime(_startedTimestamp) < TimeSpan.FromSeconds(_request.DurationSeconds))
        {
            cancellationToken.ThrowIfCancellationRequested();
            _statistics.RecordScheduled();
            yield return new PaymentRequest(_request.Amount);
            await Task.Delay(TimeSpan.FromSeconds(1d / _request.RequestsPerSecond), cancellationToken);
        }
    }

    private static async Task SendRequestAsync(
        HttpClient client,
        PaymentRequest request,
        TrafficStatistics statistics,
        CancellationToken cancellationToken)
    {
        var startedAt = Stopwatch.GetTimestamp();
        try
        {
            using var response = await client.PostAsJsonAsync(
                "/api/payments",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                statistics.RecordHttpFailure();
                return;
            }

            statistics.RecordHttpSuccess();
            var result = await response.Content.ReadFromJsonAsync<PaymentResult>(cancellationToken);
            statistics.RecordOutcome(result?.Status, result?.RiskPod);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            statistics.RecordError();
        }
        catch (HttpRequestException)
        {
            statistics.RecordError();
        }
        catch (System.Text.Json.JsonException)
        {
            statistics.RecordInvalidResponse();
        }
        finally
        {
            statistics.RecordLatency(Stopwatch.GetElapsedTime(startedAt));
        }
    }

    private sealed record PaymentRequest(decimal Amount);

    private sealed record PaymentResult(
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("riskPod")] string? RiskPod);
}

internal sealed class TrafficStatistics
{
    private readonly ConcurrentBag<double> _latenciesMilliseconds = new();
    private readonly ConcurrentDictionary<string, int> _riskEngineInstances = new(StringComparer.OrdinalIgnoreCase);
    private long _scheduled;
    private long _responses;
    private long _successfulResponses;
    private long _failedResponses;
    private long _completedRequests;
    private long _errors;
    private long _approved;
    private long _rejected;
    private long _declined;

    public void RecordScheduled() => Interlocked.Increment(ref _scheduled);
    public void RecordHttpSuccess()
    {
        Interlocked.Increment(ref _responses);
        Interlocked.Increment(ref _successfulResponses);
        Interlocked.Increment(ref _completedRequests);
    }

    public void RecordHttpFailure()
    {
        Interlocked.Increment(ref _responses);
        Interlocked.Increment(ref _failedResponses);
        RecordError();
    }

    public void RecordError()
    {
        Interlocked.Increment(ref _errors);
        Interlocked.Increment(ref _completedRequests);
    }

    public void RecordInvalidResponse() => Interlocked.Increment(ref _errors);
    public void RecordLatency(TimeSpan latency) => _latenciesMilliseconds.Add(latency.TotalMilliseconds);

    public void RecordOutcome(string? status, string? riskEngineInstance)
    {
        if (!string.IsNullOrWhiteSpace(riskEngineInstance))
            _riskEngineInstances.AddOrUpdate(riskEngineInstance, 1, static (_, count) => count + 1);

        if (status?.Equals("Approved", StringComparison.OrdinalIgnoreCase) == true)
            Interlocked.Increment(ref _approved);
        else if (status?.Equals("Rejected", StringComparison.OrdinalIgnoreCase) == true)
            Interlocked.Increment(ref _rejected);
        else if (status?.Equals("Declined", StringComparison.OrdinalIgnoreCase) == true)
            Interlocked.Increment(ref _declined);
    }

    public TrafficStatisticsSnapshot GetSnapshot(TimeSpan elapsed)
    {
        var latencies = _latenciesMilliseconds.Order().ToArray();
        var scheduled = Interlocked.Read(ref _scheduled);
        var responses = Interlocked.Read(ref _responses);
        var successes = Interlocked.Read(ref _successfulResponses);
        var failures = Interlocked.Read(ref _failedResponses);
        var errors = Interlocked.Read(ref _errors);
        var completedRequests = Interlocked.Read(ref _completedRequests);
        var canceled = Math.Max(0, scheduled - completedRequests);

        return new TrafficStatisticsSnapshot(
            scheduled,
            responses,
            successes,
            failures,
            errors,
            canceled,
            Interlocked.Read(ref _approved),
            Interlocked.Read(ref _rejected),
            Interlocked.Read(ref _declined),
            responses / Math.Max(elapsed.TotalSeconds, 0.001),
            latencies.Length == 0 ? null : latencies.Average(),
            latencies.Length == 0 ? null : Percentile(latencies, 0.50),
            latencies.Length == 0 ? null : Percentile(latencies, 0.95),
            new Dictionary<string, int>(_riskEngineInstances.OrderBy(pair => pair.Key)
                .ToDictionary(pair => pair.Key, pair => pair.Value), StringComparer.OrdinalIgnoreCase));
    }

    private static double Percentile(double[] sortedValues, double percentile)
    {
        var index = (int)Math.Ceiling(percentile * sortedValues.Length) - 1;
        return sortedValues[Math.Clamp(index, 0, sortedValues.Length - 1)];
    }
}
