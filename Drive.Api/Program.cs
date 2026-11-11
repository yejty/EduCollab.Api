using Drive.Api.Config;
using Drive.Api.Extensions;
using Drive.Api.Middleware;
using Drive.Api.MinimalApis;
using Drive.Api.Swagger;
using Space.Application;
using Space.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiHost();
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtOptions(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<RequestIdMiddleware>();
app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint($"/swagger/{OpenApiContractDescriptions.DocumentName}/swagger.json", "Drive API v1 (parked)");
});

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapDriveApi();
app.MapHealthChecks("/_health");

if (!app.Environment.IsEnvironment("Testing"))
{
    await app.Services.InitializeDatabaseAsync();
}

app.Run();

public partial class Program;
