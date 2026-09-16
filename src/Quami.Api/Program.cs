using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Quami.Api.Authentication;
using Quami.Infrastructure;
using Quami.Infrastructure.Persistence;

// QUAMI API
// Bu fazda İÇİ BOŞ. Amacı hazır durmak: sonraki fazda Python yapay zekâ
// servisi ve Jira entegrasyonu buraya bağlanacak. Bugün yalnızca ayakta
// olduğunu gösteren sağlık kontrolü ve API anahtarı doğrulaması var.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Veri katmanı. Oturum yoktur; kiracı bağlamı boş uygulamadır (NullTenantContext).
// Kiracıya göre çalışan uçlar eklendiğinde kiracı, API anahtarına bağlanacak.
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<ApiKeyOptions>(builder.Configuration.GetSection(ApiKeyOptions.SectionName));

builder.Services
    .AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler.SchemeName, _ => { });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

// Sağlık kontrolü: anahtar istemez, izleme araçları çağırabilsin.
// Veritabanına erişilebiliyor mu diye de bakar.
app.MapGet("/health", async (QuamiDbContext db, CancellationToken cancellationToken) =>
{
    bool databaseReachable;
    try
    {
        databaseReachable = await db.Database.CanConnectAsync(cancellationToken);
    }
    catch
    {
        databaseReachable = false;
    }

    var payload = new
    {
        status = databaseReachable ? "healthy" : "degraded",
        database = databaseReachable ? "up" : "down",
        utc = DateTime.UtcNow
    };

    return databaseReachable ? Results.Ok(payload) : Results.Json(payload, statusCode: 503);
})
.AllowAnonymous();

// Anahtarın doğru kurulduğunu bağlanan tarafın sınayabilmesi için.
// İş mantığı yok; asıl uçlar sonraki fazda eklenecek.
app.MapGet("/api/ping", (HttpContext http) => Results.Ok(new
{
    client = http.User.Identity?.Name,
    utc = DateTime.UtcNow
}))
.RequireAuthorization();

app.Run();
