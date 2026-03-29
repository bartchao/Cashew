namespace CashewAPI.Services;

public interface IGoogleDriveService
{
    Task<(byte[] fileBytes, string fileName)> DownloadLatestCashewDb(string? fileId = null);
    Task<string> UploadCashewDb(byte[] fileBytes, string fileName);
}
