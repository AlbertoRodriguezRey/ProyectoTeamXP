using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace ProyectoTeamXP.Services;

/// <summary>
/// Servicio para descargar archivos de Google Drive.
/// Soporta Google Sheets nativo (lo convierte a .xlsx) y archivos .xlsx directos.
/// </summary>
public class GoogleDriveService
{
    private readonly IConfiguration _config;

    public GoogleDriveService(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Descarga un archivo de Drive como .xlsx en memoria.
    /// Si es Google Sheets nativo → Files.Export a .xlsx.
    /// Si ya es .xlsx → Files.Get descarga directa.
    /// </summary>
    public async Task<MemoryStream> DescargarComoExcelAsync(string fileId)
    {
        var service = CrearDriveService();

        // Obtener metadata para saber el tipo MIME
        var request = service.Files.Get(fileId);
        request.Fields = "id, name, mimeType";
        var fileMeta = await request.ExecuteAsync();

        var stream = new MemoryStream();

        if (fileMeta.MimeType == "application/vnd.google-apps.spreadsheet")
        {
            // Google Sheets nativo → exportar como .xlsx (preserva merges, fórmulas, formato)
            var exportRequest = service.Files.Export(fileId,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            await exportRequest.DownloadAsync(stream);
        }
        else
        {
            // Ya es un archivo binario (.xlsx, .xls) → descarga directa
            var downloadRequest = service.Files.Get(fileId);
            await downloadRequest.DownloadAsync(stream);
        }

        if (stream.Length == 0)
            throw new InvalidOperationException(
                $"No se pudo descargar el archivo '{fileMeta.Name}' (tipo: {fileMeta.MimeType}). " +
                "Verifica que el Service Account tenga acceso al archivo.");

        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// Obtiene el nombre del archivo en Drive.
    /// </summary>
    public async Task<string> ObtenerNombreArchivoAsync(string fileId)
    {
        var service = CrearDriveService();
        var request = service.Files.Get(fileId);
        request.Fields = "name";
        var file = await request.ExecuteAsync();
        return file.Name;
    }

    private DriveService CrearDriveService()
    {
        // Ruta al JSON de Service Account (configurable en appsettings.json)
        var credentialsPath = _config["GoogleDrive:ServiceAccountJsonPath"]
            ?? throw new InvalidOperationException(
                "Falta la configuración 'GoogleDrive:ServiceAccountJsonPath' en appsettings.json. " +
                "Debe apuntar al archivo JSON de tu Service Account de Google Cloud.");

        if (!File.Exists(credentialsPath))
            throw new FileNotFoundException(
                $"No se encontró el archivo de credenciales: {credentialsPath}. " +
                "Descárgalo desde Google Cloud Console → IAM → Service Accounts → Keys.");

        GoogleCredential credential;
        using (var fileStream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential
                .FromStream(fileStream)
                .CreateScoped(DriveService.ScopeConstants.DriveReadonly);
        }

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "ProyectoTeamXP"
        });
    }
}
