using System.Diagnostics;

internal static class CpuWorkload
{
    private const int BatchSize = 4096;
    private static long _resultSink;

    public static CpuWorkloadMeasurement Run(
        int durationMilliseconds,
        CancellationToken cancellationToken)
    {
        if (durationMilliseconds == 0)
            return new CpuWorkloadMeasurement(0, 0);

        var stopwatch = Stopwatch.StartNew();
        ulong state = 0x9E3779B97F4A7C15;
        long iterations = 0;

        while (stopwatch.ElapsedMilliseconds < durationMilliseconds)
        {
            for (var i = 0; i < BatchSize; i++)
            {
                state ^= state << 13;
                state ^= state >> 7;
                state ^= state << 17;
                iterations++;
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        stopwatch.Stop();
        Interlocked.Exchange(ref _resultSink, unchecked((long)state));

        return new CpuWorkloadMeasurement(stopwatch.ElapsedMilliseconds, iterations);
    }
}

internal sealed record CpuWorkloadMeasurement(long ElapsedMilliseconds, long Iterations);
