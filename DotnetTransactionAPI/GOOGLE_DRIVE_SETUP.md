# Google Drive Setup Guide for CashewAPI

This guide walks you through configuring the CashewAPI to access a Cashew SQLite database file stored on Google Drive. The API uses a Google Cloud **Service Account** to authenticate with the Google Drive API, download your Cashew backup, and serve transaction data through a REST API.

---

## Prerequisites

Before you begin, make sure you have the following:

- A **Google Account** with access to [Google Cloud Console](https://console.cloud.google.com/).
- The **Cashew** Flutter app installed, with at least one database backup on Google Drive (the app backs up files named like `cashew-2024-01-15-143022.sql`).
- The CashewAPI project cloned and ready to run (see the main [README](../README.md) or [SPEC.md](SPEC.md) for build instructions).
- **.NET 10 SDK** installed (or Docker, if running via container).

---

## Step 1: Create a Google Cloud Project

1. Go to the [Google Cloud Console](https://console.cloud.google.com/).
2. Click the **project selector** dropdown at the top of the page (next to "Google Cloud").
3. Click **New Project**.
4. Enter a project name, for example: `Cashew API`.
5. (Optional) Select an organization or leave it as "No organization".
6. Click **Create**.
7. Wait a few seconds, then select your newly created project from the project selector.

> **Tip:** If you already have a Google Cloud project you'd like to reuse, you can skip this step and use the existing project.

---

## Step 2: Enable the Google Drive API

1. In your Google Cloud project, navigate to **APIs & Services** → **Library** (or go directly to [API Library](https://console.cloud.google.com/apis/library)).
2. Search for **Google Drive API**.
3. Click on **Google Drive API** in the results.
4. Click **Enable**.

The Drive API is now active for your project. The CashewAPI uses the `Google.Apis.Drive.v3` NuGet package to interact with this API.

---

## Step 3: Create a Service Account

A service account is a special Google account that belongs to your project rather than to an individual user. The CashewAPI authenticates as this service account to access files shared with it.

### 3.1 Create the Service Account

1. In Google Cloud Console, go to **IAM & Admin** → **Service Accounts** (or visit [Service Accounts](https://console.cloud.google.com/iam-admin/serviceaccounts)).
2. Click **+ Create Service Account**.
3. Fill in the details:
   - **Service account name:** `cashew-api` (or any descriptive name)
   - **Service account ID:** This auto-fills based on the name (e.g., `cashew-api@your-project-id.iam.gserviceaccount.com`)
   - **Description:** `Service account for CashewAPI to access Cashew database backups on Google Drive`
4. Click **Create and Continue**.
5. (Optional) You can skip the "Grant this service account access to project" step — no additional project roles are needed.
6. (Optional) Skip the "Grant users access to this service account" step.
7. Click **Done**.

### 3.2 Download the Credentials JSON Key

1. In the Service Accounts list, click on the service account you just created.
2. Go to the **Keys** tab.
3. Click **Add Key** → **Create new key**.
4. Select **JSON** as the key type.
5. Click **Create**.

A JSON file will download automatically. This file contains the private key for your service account. **Keep this file secure.**

6. Rename the downloaded file to `credentials.json` for convenience.
7. Note the **service account email address** — you will need it in the next step. It looks like:
   ```
   cashew-api@your-project-id.iam.gserviceaccount.com
   ```

> ⚠️ **Important:** This JSON key file grants access to your Google Drive files. Treat it like a password. See [Security Best Practices](#security-best-practices) below.

---

## Step 4: Share Your Cashew Database File with the Service Account

The CashewAPI uses the `DriveFile` scope, which limits access to files that are **created by or explicitly shared with** the service account. Since the Cashew Flutter app backs up the database to your personal Google Drive, you must share the backup file with the service account.

### 4.1 Find the Cashew Backup File

1. Open [Google Drive](https://drive.google.com/) in your browser.
2. Search for your Cashew backup file. Backup files created by the Cashew app are typically named:
   ```
   cashew-2024-01-15-143022.sql
   ```
   The naming pattern is `cashew-YYYY-MM-DD-HHmmss.sql` (or `.sqlite`).
3. If you have multiple backups, share the **most recent** one (or share all of them — the API automatically selects the latest by modification date).

### 4.2 Share the File

1. Right-click the Cashew backup file and select **Share** → **Share**.
2. In the "Add people, groups, and calendar events" field, paste the **service account email address**:
   ```
   cashew-api@your-project-id.iam.gserviceaccount.com
   ```
3. Set the permission level:
   - **Viewer** — if you only need the API to read data (recommended for most use cases).
   - **Editor** — if you also want the API to upload/push modified databases back to Drive.
4. Uncheck "Notify people" (service accounts cannot receive email notifications).
5. Click **Share** (or **Send**).

> **Tip:** If your Cashew backups are stored inside a folder, you can share the entire folder with the service account instead of individual files.

---

## Step 5: Configure the API

The CashewAPI reads the path to the service account credentials JSON file from configuration. There are three ways to provide this.

### Option A: Using `appsettings.json`

Edit the `CashewAPI/appsettings.json` file (or create an `appsettings.Development.json` for local development):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ApiKey": "your-secret-api-key",
  "DatabaseDirectory": "./data",
  "GoogleDrive": {
    "CredentialPath": "/path/to/your/credentials.json"
  }
}
```

Replace `/path/to/your/credentials.json` with the actual path to the JSON key file you downloaded in Step 3.

> **Note:** Prefer `appsettings.Development.json` for local development. This file is already listed in `.gitignore` and will not be committed to source control.

### Option B: Using Environment Variables

ASP.NET Core automatically maps double-underscore (`__`) separators to colon (`:`) in hierarchical config keys. Set the following environment variable:

**Linux / macOS:**
```bash
export GoogleDrive__CredentialPath="/path/to/your/credentials.json"
export ApiKey="your-secret-api-key"
```

**Windows (PowerShell):**
```powershell
$env:GoogleDrive__CredentialPath = "C:\path\to\your\credentials.json"
$env:ApiKey = "your-secret-api-key"
```

Then run the API:
```bash
dotnet run --project CashewAPI
```

### Option C: Using Docker

The included `docker-compose.yml` is pre-configured for credential mounting. Follow these steps:

1. **Place your credentials file** in a `credentials/` directory next to the `docker-compose.yml`:
   ```
   DotnetTransactionAPI/
   ├── credentials/
   │   └── credentials.json    ← your service account key
   ├── docker-compose.yml
   ├── Dockerfile
   └── ...
   ```

2. **Set your API key** by creating a `.env` file or exporting it:
   ```bash
   export CASHEW_API_KEY="your-secret-api-key"
   ```

3. **Start the container:**
   ```bash
   docker compose up -d
   ```

The `docker-compose.yml` mounts the `./credentials` directory read-only into the container at `/app/credentials` and sets `GoogleDrive__CredentialPath=/app/credentials/credentials.json` automatically.

Relevant excerpt from `docker-compose.yml`:
```yaml
services:
  cashew-api:
    environment:
      - ApiKey=${CASHEW_API_KEY:-changeme}
      - DatabaseDirectory=/app/data
      - GoogleDrive__CredentialPath=/app/credentials/credentials.json
    volumes:
      - cashew-data:/app/data
      - ./credentials:/app/credentials:ro
```

---

## Step 6: Verify the Setup

Once the API is running, verify that Google Drive integration is working correctly.

### 6.1 Pull the Database from Google Drive

Send a `POST` request to download and load the latest Cashew database:

```bash
curl -X POST http://localhost:8080/api/sync/pull \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: your-secret-api-key" \
  -d '{}'
```

**Expected response (200 OK):**
```json
{
  "message": "Database synced from Google Drive.",
  "fileName": "cashew-2024-01-15-143022.sql",
  "transactionCount": 256
}
```

If you know the specific Google Drive file ID, you can pass it explicitly:
```bash
curl -X POST http://localhost:8080/api/sync/pull \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: your-secret-api-key" \
  -d '{"fileId": "1aBcDeFgHiJkLmNoPqRsTuVwXyZ"}'
```

### 6.2 Check Sync Status

```bash
curl http://localhost:8080/api/sync/status \
  -H "X-Api-Key: your-secret-api-key"
```

**Expected response (200 OK):**
```json
{
  "databaseLoaded": true,
  "fileName": "cashew-2024-01-15-143022.sql",
  "lastSyncTime": "2024-01-15T14:35:00Z",
  "transactionCount": 256
}
```

### 6.3 Troubleshooting

| Symptom | Likely Cause | Solution |
|---|---|---|
| `GoogleDrive:CredentialPath configuration is required.` | The credential path is not set or is empty. | Verify that `GoogleDrive:CredentialPath` is configured in `appsettings.json` or as an environment variable. |
| `No Cashew database file found on Google Drive.` | The service account cannot see any matching files. | Ensure the Cashew backup file is **shared** with the service account email. File names must match the pattern `cashew-*.sql` or `cashew-*.sqlite`. |
| `401 Unauthorized` or credential errors | The credentials JSON file is invalid or the file path is wrong. | Double-check the file path. Re-download the key from Google Cloud Console if needed. |
| `403 Forbidden` | The Google Drive API is not enabled, or the file permissions are insufficient. | Enable the Drive API in your Google Cloud project (Step 2). Verify the file is shared with the service account. |
| `File not found` at the credential path | The JSON key file doesn't exist at the specified path. | Verify the file exists. For Docker, ensure the volume mount is correct. |
| API returns `401` for your HTTP request | Missing or incorrect `X-Api-Key` header. | Include the `X-Api-Key` header with the value matching your configured `ApiKey`. |

---

## Security Best Practices

### Never Commit Credentials to Source Control

The `credentials.json` file is already listed in the project's `.gitignore`. Verify it stays excluded:

```gitignore
credentials.json
appsettings.Development.json
```

If you accidentally commit credentials, **revoke the key immediately** in Google Cloud Console and create a new one.

### Use Least-Privilege Permissions

- The CashewAPI uses the `DriveFile` scope (`https://www.googleapis.com/auth/drive.file`), which is the most restrictive Drive scope — it only allows access to files created by or explicitly shared with the service account.
- Share individual files rather than entire folders when possible.
- Use **Viewer** permission unless the API needs to upload (push) files, in which case use **Editor**.

### Rotate Service Account Keys Periodically

Google recommends rotating service account keys regularly:

1. Go to **IAM & Admin** → **Service Accounts** in Google Cloud Console.
2. Select your service account.
3. Under the **Keys** tab, click **Add Key** → **Create new key** to generate a new key.
4. Update the `credentials.json` file in your deployment.
5. After verifying the new key works, **delete the old key** from the Keys tab.

### Additional Recommendations

- **Restrict key usage:** Consider using [VPC Service Controls](https://cloud.google.com/vpc-service-controls) or [IAM Conditions](https://cloud.google.com/iam/docs/conditions-overview) to further limit how the service account key can be used.
- **Monitor usage:** Enable [Cloud Audit Logs](https://cloud.google.com/logging/docs/audit) to track when the service account accesses Drive files.
- **Use secrets management:** In production, consider storing the credentials in a secrets manager (e.g., Google Secret Manager, Azure Key Vault, HashiCorp Vault) rather than as a plain file on disk.
- **Set an API key:** Always set a strong, unique value for the `ApiKey` configuration. The API uses this key to authenticate incoming requests via the `X-Api-Key` header.
