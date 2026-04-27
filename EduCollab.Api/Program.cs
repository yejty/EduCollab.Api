using EduCollab.Api.Config;
using EduCollab.Api.Extensions;
using EduCollab.Application;
using EduCollab.Application.Database;

var builder = WebApplication.CreateBuilder(args);

var databaseOptions = builder.Configuration
    .GetSection(DatabaseOptions.SectionName)
    .Get<DatabaseOptions>()
    ?? throw new InvalidOperationException($"Configuration section '{DatabaseOptions.SectionName}' is missing.");

builder.Services.AddApiHost();
builder.Services.AddApplication();
builder.Services.AddDatabase(databaseOptions);
builder.Services.AddJwtOptions(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/_health");

await app.Services.InitializeDatabaseAsync();

app.Run();
