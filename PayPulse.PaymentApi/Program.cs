
using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure communication with the other microservices.
builder.Services.AddHttpClient("RiskEngine", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ServiceUrls:RiskEngine"]!);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient("PaymentProcessor", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ServiceUrls:PaymentProcessor"]!);
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

app.MapPost("/api/payments", async (
    PaymentRequest request,
    IHttpClientFactory httpClientFactory) =>
{
    if (request.Amount <= 0)
        return Results.BadRequest("Amount must be greater than zero.");

    // Generate a simulated payment ID.
    long paymentId = Random.Shared.NextInt64(1, 1_000_000);

    try
    {
        // STEP 1: Evaluate fraud risk.
        var riskClient = httpClientFactory.CreateClient("RiskEngine");

        var riskResponse = await riskClient.PostAsJsonAsync(
            "/api/risk/evaluate",
            new { PaymentId = paymentId, request.Amount });

        riskResponse.EnsureSuccessStatusCode();

        var risk = await riskResponse.Content
            .ReadFromJsonAsync<RiskResult>();

        if (risk is null)
            return Results.Problem("Invalid RiskEngine response.");

        // STEP 2: Reject suspicious payments.
        if (!risk.Approved)
        {
            return Results.Ok(new
            {
                PaymentId = paymentId,
                request.Amount,
                Status = "Rejected",
                risk.RiskScore,
                RiskPod = risk.ProcessedBy
            });
        }

        // STEP 3: Simulate bank authorization.
        var processorClient =
            httpClientFactory.CreateClient("PaymentProcessor");

        var paymentResponse = await processorClient.PostAsJsonAsync(
            "/api/payments/process",
            new { PaymentId = paymentId, request.Amount });

        paymentResponse.EnsureSuccessStatusCode();

        var payment = await paymentResponse.Content
            .ReadFromJsonAsync<ProcessorResult>();

        if (payment is null)
            return Results.Problem("Invalid PaymentProcessor response.");

        // STEP 4: Return the final payment result.
        return Results.Ok(new
        {
            PaymentId = paymentId,
            request.Amount,
            payment.Status,
            risk.RiskScore,
            RiskPod = risk.ProcessedBy,
            ProcessorPod = payment.ProcessedBy
        });
    }
    catch (HttpRequestException)
    {
        return Results.Problem(
            "A payment service is unavailable.",
            statusCode: 502);
    }
    catch (TaskCanceledException)
    {
        return Results.Problem(
            "A payment service timed out.",
            statusCode: 504);
    }
});

app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();

// Request and response models.
record PaymentRequest(decimal Amount);

record RiskResult(
    long PaymentId,
    int RiskScore,
    bool Approved,
    string ProcessedBy);

record ProcessorResult(
    long PaymentId,
    decimal Amount,
    string Status,
    string ProcessedBy);
