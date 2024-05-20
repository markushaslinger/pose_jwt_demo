using System.Text.Json.Serialization;
using JwtDemo.Core;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureSwagger();
builder.Services.LoadConfiguration(builder.Configuration);
builder.Services.RegisterServices();
builder.Services.ConfigureAuth(builder.Configuration);
builder.Services.ConfigureCors(builder.Configuration);
builder.Services.ConfigureDatabase(builder.Configuration);
builder.Services.Configure<JsonOptions>(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(Setup.CorsPolicyName);
app.UseHttpsRedirection();

await app.UpdateDatabase();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
