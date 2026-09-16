using Quami.Application.Abstractions;

namespace Quami.Web.Files;

public static class FileEndpoints
{
    /// <summary>
    /// Dosya indirme ucu. <see cref="IFileStorage.GetUrl"/> bu adresi üretir.
    ///
    /// Kiracı kontrolü depo uygulamasındadır: depo, kiracıyı oturumdan okur ve
    /// yalnızca o kiracının klasörüne bakar. Başka bir kiracının depo adı
    /// yazılsa bile dosya bulunamaz.
    ///
    /// NOT: gerçek dosya adı ve içerik türü, dosyayı kaydeden modülün kendi
    /// tablosunda tutulacak. Dokümanlar modülü yazıldığında bu uç o bilgiyi
    /// kullanıp doğru adla indirecek; şimdilik genel tür döner.
    /// </summary>
    public static void MapFileEndpoints(this WebApplication app)
    {
        app.MapGet("/files/{storageKey}", async (
            string storageKey,
            IFileStorage storage,
            CancellationToken cancellationToken) =>
        {
            Stream? stream;
            try
            {
                stream = await storage.GetAsync(storageKey, cancellationToken);
            }
            catch (ArgumentException)
            {
                return Results.BadRequest();
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }

            return stream is null
                ? Results.NotFound()
                : Results.File(stream, "application/octet-stream", storageKey);
        })
        .RequireAuthorization();
    }
}
