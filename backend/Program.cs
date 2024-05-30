using System.Text.Json.Serialization;
using JwtDemo.BackgroundServices;
using JwtDemo.Core;
using JwtDemo.Hubs;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR()
       .AddJsonProtocol(o =>
       {
           o.PayloadSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
       });
builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureSwagger();
builder.Services.LoadConfiguration(builder.Configuration);
builder.Services.RegisterServices();
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddRateLimiting();
builder.Services.ConfigureCors(builder.Configuration);
builder.Services.ConfigureDatabase(builder.Configuration);
builder.Services.AddHostedService<TimeUpdateService>();
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
app.UseRateLimiter();
app.UseHttpsRedirection();

await app.UpdateDatabase();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TimeHub>("hubs/time");

app.Run();
