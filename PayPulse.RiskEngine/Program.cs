
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/api/risk/evaluate", (RiskRequest request) =>
{
    if (request.Amount <= 0)
        return Results.BadRequest("Amount must be greater than zero.");

    // Same payment ID always produces the same result.
    int riskScore = (int)(request.PaymentId % 100);

    bool isApproved = riskScore < 90;

    return Results.Ok(new
    {
        request.PaymentId,
        RiskScore = riskScore,
        Approved = isApproved,
        ProcessedBy = Environment.MachineName
    });
});

app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();

record RiskRequest(long PaymentId, decimal Amount);