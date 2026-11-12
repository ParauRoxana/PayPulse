
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<CpuWorkloadOptions>()
    .BindConfiguration(CpuWorkloadOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

app.MapPost("/api/risk/evaluate", (
    RiskRequest request,
    IOptions<CpuWorkloadOptions> workloadOptions,
    CancellationToken cancellationToken) =>
{
    if (request.Amount <= 0)
        return Results.BadRequest("Amount must be greater than zero.");

    var workload = CpuWorkload.Run(
        workloadOptions.Value.DurationMilliseconds,
        cancellationToken);

    // Same payment ID always produces the same result.
    int riskScore = (int)(request.PaymentId % 100);

    bool isApproved = riskScore < 90;

    return Results.Ok(new
    {
        request.PaymentId,
        RiskScore = riskScore,
        Approved = isApproved,
        ProcessedBy = Environment.MachineName,
        CpuWorkloadMilliseconds = workload.ElapsedMilliseconds,
        CpuWorkloadIterations = workload.Iterations
    });
});

app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();

record RiskRequest(long PaymentId, decimal Amount);
