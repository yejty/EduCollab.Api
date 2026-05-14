using EduCollab.Api.Config;
using EduCollab.Api.Extensions;
using EduCollab.Application;
using EduCollab.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiHost();
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtOptions(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

//if (app.Environment.IsDevelopment())
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/_health");

await app.Services.InitializeDatabaseAsync();

app.Run();
