using EduCollab.Api.Config;
using EduCollab.Api.Extensions;
using EduCollab.Api.Middleware;
using EduCollab.Api.Swagger;
using EduCollab.Application;
using EduCollab.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiHost();
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtOptions(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<RequestIdMiddleware>();
app.UseExceptionHandler();

//if (app.Environment.IsDevelopment())
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint($"/swagger/{OpenApiContractDescriptions.DocumentName}/swagger.json", "EduCollab API v1");
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/_health");

if (!app.Environment.IsEnvironment("Testing"))
{
    await app.Services.InitializeDatabaseAsync();
}

app.Run();

public partial class Program;
