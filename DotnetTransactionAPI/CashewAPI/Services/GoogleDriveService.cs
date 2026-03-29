using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace CashewAPI.Services;

public class GoogleDriveService : IGoogleDriveService
{
    private readonly Lazy<DriveService> _driveService;
    private readonly ILogger<GoogleDriveService> _logger;

    public GoogleDriveService(IConfiguration configuration, ILogger<GoogleDriveService> logger)
    {
        _logger = logger;

        var credentialPath = configuration.GetValue<string>("GoogleDrive:CredentialPath");

        _driveService = new Lazy<DriveService>(() =>
        {
            if (string.IsNullOrEmpty(credentialPath))
                throw new InvalidOperationException("GoogleDrive:CredentialPath configuration is required.");

            var credential = CredentialFactory.FromFile<GoogleCredential>(credentialPath)
                .CreateScoped(DriveService.ScopeConstants.DriveFile);

            return new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "CashewAPI",
            });
        });
    }

    public async Task<(byte[] fileBytes, string fileName)> DownloadLatestCashewDb(string? fileId = null)
    {
        if (!string.IsNullOrEmpty(fileId))
        {
            return await DownloadFileById(fileId);
        }

        // Search for the latest cashew-*.sql file
        var listRequest = _driveService.Value.Files.List();
        listRequest.Q = "name contains 'cashew-' and (name contains '.sql' or name contains '.sqlite')";
        listRequest.OrderBy = "modifiedTime desc";
        listRequest.PageSize = 1;
        listRequest.Fields = "files(id, name, modifiedTime)";

        var result = await listRequest.ExecuteAsync();
        if (result.Files == null || result.Files.Count == 0)
        {
            throw new FileNotFoundException("No Cashew database file found on Google Drive.");
        }

        var file = result.Files[0];
        _logger.LogInformation("Found Cashew DB file: {FileName} (ID: {FileId})", file.Name, file.Id);

        return await DownloadFileById(file.Id, file.Name);
    }

    public async Task<string> UploadCashewDb(byte[] fileBytes, string fileName)
    {
        var fileMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = fileName,
        };

        using var stream = new MemoryStream(fileBytes);
        var request = _driveService.Value.Files.Create(fileMetadata, stream, "application/x-sqlite3");
        request.Fields = "id, name";

        var result = await request.UploadAsync();
        if (result.Status != Google.Apis.Upload.UploadStatus.Completed)
        {
            throw new InvalidOperationException($"Failed to upload file: {result.Exception?.Message}");
        }

        var uploadedFile = request.ResponseBody;
        _logger.LogInformation("Uploaded Cashew DB: {FileName} (ID: {FileId})", uploadedFile.Name, uploadedFile.Id);

        return uploadedFile.Id;
    }

    private async Task<(byte[] fileBytes, string fileName)> DownloadFileById(string fileId, string? knownFileName = null)
    {
        // Get file metadata if name not known
        if (string.IsNullOrEmpty(knownFileName))
        {
            var fileMetadata = await _driveService.Value.Files.Get(fileId).ExecuteAsync();
            knownFileName = fileMetadata.Name;
        }

        var getRequest = _driveService.Value.Files.Get(fileId);
        using var memoryStream = new MemoryStream();
        await getRequest.DownloadAsync(memoryStream);

        _logger.LogInformation("Downloaded {FileName} ({Size} bytes)", knownFileName, memoryStream.Length);

        return (memoryStream.ToArray(), knownFileName);
    }
}
