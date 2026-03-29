// IGoogleDriveService.cs — Interface for Google Drive file operations used by the sync endpoints.

namespace CashewAPI.Services;

/// <summary>
/// Abstraction for downloading and uploading Cashew SQLite database files via Google Drive.
/// </summary>
public interface IGoogleDriveService
{
    /// <summary>
    /// Downloads the latest Cashew database file from Google Drive.
    /// </summary>
    /// <param name="fileId">Optional specific file ID. If omitted, the most recently modified cashew-*.sql file is used.</param>
    /// <returns>A tuple of raw file bytes and the original file name.</returns>
    Task<(byte[] fileBytes, string fileName)> DownloadLatestCashewDb(string? fileId = null);

    /// <summary>
    /// Uploads a Cashew database file to Google Drive.
    /// </summary>
    /// <param name="fileBytes">Raw SQLite database content.</param>
    /// <param name="fileName">Name for the uploaded file.</param>
    /// <returns>The Google Drive file ID of the uploaded file.</returns>
    Task<string> UploadCashewDb(byte[] fileBytes, string fileName);
}
