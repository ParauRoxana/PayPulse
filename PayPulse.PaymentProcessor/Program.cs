
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/api/payments/process", (PaymentRequest request) =>
{
    if (request.Amount <= 0)
        return Results.BadRequest("Amount must be greater than zero.");

    // Simulate bank authorization.
    bool approved = request.PaymentId % 10 != 0;

    return Results.Ok(new
    {
        request.PaymentId,
        request.Amount,
        Status = approved ? "Approved" : "Declined",
        ProcessedBy = Environment.MachineName
    });
});

app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();

record PaymentRequest(long PaymentId, decimal Amount);
