var builder = WebApplication.CreateBuilder(args);

var paymentApiBaseUrl = builder.Configuration["Traffic:PaymentApiBaseUrl"] ?? "https://localhost:7216";
if (!Uri.TryCreate(paymentApiBaseUrl, UriKind.Absolute, out var paymentApiBaseUri) ||
    paymentApiBaseUri.Scheme is not ("http" or "https"))
{
    throw new InvalidOperationException("Traffic:PaymentApiBaseUrl must be an absolute HTTP or HTTPS URL.");
}

builder.Services.AddHttpClient(BlackFridayTrafficRun.PaymentApiClientName, client =>
{
    client.BaseAddress = paymentApiBaseUri;
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddSingleton<BlackFridayTrafficRunManager>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    if (allowedOrigins.Length > 0)
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
}));

var app = builder.Build();
app.UseCors("Frontend");

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.MapPost("/api/traffic-runs", (StartTrafficRequest request, BlackFridayTrafficRunManager manager) =>
{
    var validationErrors = request.Validate();
    if (validationErrors.Count > 0)
        return Results.ValidationProblem(validationErrors);

    var run = manager.Start(request);
    if (run is null)
        return Results.Conflict(new { error = "A traffic run is already active.", activeRun = manager.GetActive() });

    return Results.Accepted($"/api/traffic-runs/{run.RunId}", run.GetSnapshot());
});

app.MapGet("/api/traffic-runs/active", (BlackFridayTrafficRunManager manager) =>
{
    var activeRun = manager.GetActive();
    if (activeRun is null)
        return (IResult)Results.NoContent();
    return Results.Ok(activeRun);
});

app.MapGet("/api/traffic-runs/{runId:guid}", (Guid runId, BlackFridayTrafficRunManager manager) =>
{
    var run = manager.Get(runId);
    if (run is null)
        return (IResult)Results.NotFound();
    return Results.Ok(run.GetSnapshot());
});

app.MapPost("/api/traffic-runs/{runId:guid}/stop", (Guid runId, BlackFridayTrafficRunManager manager) =>
{
    var run = manager.Get(runId);
    if (run is null)
        return Results.NotFound();

    run.RequestStop();
    return Results.Ok(run.GetSnapshot());
});

app.Run();
