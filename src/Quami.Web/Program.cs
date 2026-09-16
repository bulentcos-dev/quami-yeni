using Quami.Web.Components;

using Quami.Infrastructure;
using Quami.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

// Veri katmanı (DbContext, Npgsql, snake_case). ITenantContext kaydı adım 6'da.
builder.Services.AddInfrastructure(builder.Configuration);

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

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Açılışta seed: sistem geneli menü/modül kayıtları koda göre güncellenir,
// boş veritabanında örnek kiracı oluşturulur. Migration'lar yalnızca
// geliştirmede uygulanır; test/canlıda dağıtım adımı uygular.
await DatabaseSeeder.MigrateAndSeedAsync(app.Services, applyMigrations: app.Environment.IsDevelopment());

app.Run();
