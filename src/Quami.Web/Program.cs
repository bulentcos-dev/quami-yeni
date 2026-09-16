using Quami.Web.Components;

using Microsoft.AspNetCore.Localization;
using Quami.Infrastructure;
using Quami.Infrastructure.Persistence.Seed;
using Quami.Web.Localization;

var builder = WebApplication.CreateBuilder(args);

// Veri katmanı (DbContext, Npgsql, snake_case).
builder.Services.AddInfrastructure(builder.Configuration);

// Kimlik, oturum çerezi ve oturumdan beslenen kiracı bağlamı.
builder.Services.AddQuamiIdentity();
builder.Services.AddCascadingAuthenticationState();

// Çok dillilik: metinler Resources/SharedResource*.resx dosyalarından gelir.
// Varsayılan Türkçe. Sıra: çerez (açık seçim) → kullanıcının kayıtlı dili.
// Tarayıcının Accept-Language başlığı bilerek kullanılmaz; davranış öngörülebilir olsun.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture(CultureEndpoints.SupportedCultures[0])
        .AddSupportedCultures(CultureEndpoints.SupportedCultures)
        .AddSupportedUICultures(CultureEndpoints.SupportedCultures);

    options.RequestCultureProviders =
    [
        new CookieRequestCultureProvider(),
        new UserClaimCultureProvider()
    ];
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
// Kimlikten SONRA: kullanıcının kayıtlı dilini okuyabilmek için oturum gerekli.
app.UseRequestLocalization();
app.UseAuthorization();
app.UseAntiforgery();

app.MapCultureEndpoint();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Açılışta seed: sistem geneli menü/modül kayıtları koda göre güncellenir,
// boş veritabanında örnek kiracı oluşturulur. Migration'lar yalnızca
// geliştirmede uygulanır; test/canlıda dağıtım adımı uygular.
// Örnek kullanıcının parolası YALNIZCA geliştirmede açılır.
var sampleUserPassword = app.Environment.IsDevelopment()
    ? builder.Configuration["Seed:SampleUserPassword"] ?? "<GELISTIRME-PAROLASI-KALDIRILDI>"
    : null;

await DatabaseSeeder.MigrateAndSeedAsync(
    app.Services,
    applyMigrations: app.Environment.IsDevelopment(),
    sampleUserPassword: sampleUserPassword);

app.Run();
