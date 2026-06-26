var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health");

app.MapGet("/v1/system/info", (IHostEnvironment environment) =>
    Results.Ok(new SystemInfoResponse(
        "Alsappan",
        environment.EnvironmentName,
        ["pt-BR", "en-US"])))
    .WithName("GetSystemInfo");

app.Run();

#pragma warning disable CA1515
public partial class Program;
#pragma warning restore CA1515

internal sealed record SystemInfoResponse(
    string Application,
    string Environment,
    string[] SupportedCultures);
