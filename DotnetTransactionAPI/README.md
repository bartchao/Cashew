# CashewAPI

A .NET Minimal API for managing [Cashew](https://github.com/jameskoko662/Cashew) financial data through a REST interface, with Google Drive synchronisation for the SQLite database.

> 🌐 [繁體中文版](#cashewapi-繁體中文)

---

## Features

- **Transaction CRUD** — Create, read, and delete transactions (single or batch)
- **Category & Wallet queries** — List categories and wallets from the Cashew database
- **Google Drive sync** — Pull the latest Cashew SQLite backup from Google Drive, and push changes back
- **Swagger UI** — Interactive API documentation available in development mode at `/swagger`
- **API Key authentication** — Secure all endpoints with a configurable API key
- **Docker support** — Ready-to-use Dockerfile and docker-compose.yml

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or Docker)
- A Google Cloud service account with Drive API enabled (see [GOOGLE_DRIVE_SETUP.md](GOOGLE_DRIVE_SETUP.md))
- A Cashew SQLite backup file shared with the service account on Google Drive

## Quick Start

### Run locally

```bash
cd DotnetTransactionAPI

# Restore and build
dotnet build CashewAPI/CashewAPI.csproj

# Configure (edit appsettings.json or use environment variables)
export ApiKey="your-secret-api-key"
export GoogleDrive__CredentialPath="/path/to/credentials.json"

# Run
dotnet run --project CashewAPI
```

The API starts at `http://localhost:5000` (or the port configured in `launchSettings.json`).

### Run with Docker

```bash
cd DotnetTransactionAPI

# Place your service account key at credentials/credentials.json
mkdir -p credentials
cp /path/to/your/credentials.json credentials/

# Set the API key
export CASHEW_API_KEY="your-secret-api-key"

# Build and start
docker compose up -d
```

The API is available at `http://localhost:8080`.

## API Endpoints

All endpoints require the `X-Api-Key` header.

### Transactions

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/transactions` | List transactions (paginated, filterable) |
| `GET` | `/api/transactions/{pk}` | Get a single transaction |
| `POST` | `/api/transactions` | Create a transaction |
| `POST` | `/api/transactions/batch` | Batch create (up to 1000) |
| `DELETE` | `/api/transactions/{pk}` | Delete a transaction |
| `POST` | `/api/transactions/batch-delete` | Batch delete (up to 1000) |

### Categories & Wallets

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/categories` | List all categories |
| `GET` | `/api/categories/{pk}` | Get a single category |
| `GET` | `/api/wallets` | List all wallets |
| `GET` | `/api/wallets/{pk}` | Get a single wallet |

### Sync

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/sync/pull` | Download latest DB from Google Drive |
| `POST` | `/api/sync/push` | Upload current DB to Google Drive |
| `GET` | `/api/sync/status` | Check sync status |

## Swagger UI

In development mode, Swagger UI is available at:

```
http://localhost:<port>/swagger
```

The OpenAPI document is served at `/openapi/v1.json`.

## Configuration

| Key | Environment Variable | Description |
|-----|---------------------|-------------|
| `ApiKey` | `ApiKey` | API key for request authentication |
| `DatabaseDirectory` | `DatabaseDirectory` | Directory to store the local SQLite file |
| `GoogleDrive:CredentialPath` | `GoogleDrive__CredentialPath` | Path to the Google service account JSON key |

## Testing

```bash
cd DotnetTransactionAPI
dotnet test
```

35 tests cover database operations and API endpoint behaviour.

## Documentation

- [SPEC.md](SPEC.md) — Functional specification and technical design (中文)
- [GOOGLE_DRIVE_SETUP.md](GOOGLE_DRIVE_SETUP.md) — Step-by-step Google Drive API setup guide

## Project Structure

```
DotnetTransactionAPI/
├── CashewAPI/
│   ├── Program.cs              # Application entry point
│   ├── Endpoints/              # Minimal API endpoint definitions
│   ├── Services/               # Database and Google Drive services
│   ├── Models/                 # Domain and API request/response models
│   └── Middleware/             # API key authentication middleware
├── CashewAPI.Tests/            # xUnit integration tests
├── Dockerfile                  # Multi-stage Docker build
├── docker-compose.yml          # Docker Compose configuration
├── SPEC.md                     # Specification document
└── GOOGLE_DRIVE_SETUP.md       # Google Drive setup guide
```

---

# CashewAPI 繁體中文

一個 .NET Minimal API，透過 REST 介面管理 [Cashew](https://github.com/jameskoko662/Cashew) 記帳 App 的財務資料，並支援與 Google Drive 同步 SQLite 資料庫。

## 功能特色

- **交易 CRUD** — 新增、查詢、刪除交易（支援單筆與批次操作）
- **分類與錢包查詢** — 列出 Cashew 資料庫中的分類與錢包
- **Google Drive 同步** — 從 Google Drive 拉取最新的 Cashew SQLite 備份，並推送變更回去
- **Swagger UI** — 開發模式下可於 `/swagger` 使用互動式 API 文件
- **API Key 驗證** — 所有端點皆透過可設定的 API Key 進行安全驗證
- **Docker 支援** — 提供立即可用的 Dockerfile 與 docker-compose.yml

## 前置需求

- [.NET 10 SDK](https://dotnet.microsoft.com/download)（或 Docker）
- 已啟用 Drive API 的 Google Cloud 服務帳號（請參閱 [GOOGLE_DRIVE_SETUP.md](GOOGLE_DRIVE_SETUP.md)）
- 已與服務帳號共用的 Cashew SQLite 備份檔案（位於 Google Drive 上）

## 快速開始

### 本機執行

```bash
cd DotnetTransactionAPI

# 還原並建置
dotnet build CashewAPI/CashewAPI.csproj

# 設定（編輯 appsettings.json 或使用環境變數）
export ApiKey="your-secret-api-key"
export GoogleDrive__CredentialPath="/path/to/credentials.json"

# 執行
dotnet run --project CashewAPI
```

API 預設啟動於 `http://localhost:5000`（或 `launchSettings.json` 中設定的連接埠）。

### 使用 Docker 執行

```bash
cd DotnetTransactionAPI

# 將服務帳號金鑰放置於 credentials/credentials.json
mkdir -p credentials
cp /path/to/your/credentials.json credentials/

# 設定 API Key
export CASHEW_API_KEY="your-secret-api-key"

# 建置並啟動
docker compose up -d
```

API 可於 `http://localhost:8080` 存取。

## API 端點

所有端點皆需要 `X-Api-Key` 標頭。

### 交易

| 方法 | 路徑 | 說明 |
|------|------|------|
| `GET` | `/api/transactions` | 列出交易（分頁、可篩選） |
| `GET` | `/api/transactions/{pk}` | 取得單筆交易 |
| `POST` | `/api/transactions` | 新增交易 |
| `POST` | `/api/transactions/batch` | 批次新增（最多 1000 筆） |
| `DELETE` | `/api/transactions/{pk}` | 刪除交易 |
| `POST` | `/api/transactions/batch-delete` | 批次刪除（最多 1000 筆） |

### 分類與錢包

| 方法 | 路徑 | 說明 |
|------|------|------|
| `GET` | `/api/categories` | 列出所有分類 |
| `GET` | `/api/categories/{pk}` | 取得單一分類 |
| `GET` | `/api/wallets` | 列出所有錢包 |
| `GET` | `/api/wallets/{pk}` | 取得單一錢包 |

### 同步

| 方法 | 路徑 | 說明 |
|------|------|------|
| `POST` | `/api/sync/pull` | 從 Google Drive 下載最新資料庫 |
| `POST` | `/api/sync/push` | 將目前資料庫上傳至 Google Drive |
| `GET` | `/api/sync/status` | 檢查同步狀態 |

## Swagger UI

開發模式下，Swagger UI 可於以下網址存取：

```
http://localhost:<port>/swagger
```

OpenAPI 文件位於 `/openapi/v1.json`。

## 設定

| 設定鍵 | 環境變數 | 說明 |
|--------|----------|------|
| `ApiKey` | `ApiKey` | 用於請求驗證的 API Key |
| `DatabaseDirectory` | `DatabaseDirectory` | 本地 SQLite 檔案存放目錄 |
| `GoogleDrive:CredentialPath` | `GoogleDrive__CredentialPath` | Google 服務帳號 JSON 金鑰路徑 |

## 測試

```bash
cd DotnetTransactionAPI
dotnet test
```

共有 35 個測試涵蓋資料庫操作與 API 端點行為。

## 文件

- [SPEC.md](SPEC.md) — 功能規格與技術規劃（中文）
- [GOOGLE_DRIVE_SETUP.md](GOOGLE_DRIVE_SETUP.md) — Google Drive API 設定指南（英文）

## 專案結構

```
DotnetTransactionAPI/
├── CashewAPI/
│   ├── Program.cs              # 應用程式進入點
│   ├── Endpoints/              # Minimal API 端點定義
│   ├── Services/               # 資料庫與 Google Drive 服務
│   ├── Models/                 # 領域模型與 API 請求/回應模型
│   └── Middleware/             # API Key 驗證中介層
├── CashewAPI.Tests/            # xUnit 整合測試
├── Dockerfile                  # 多階段 Docker 建置
├── docker-compose.yml          # Docker Compose 設定
├── SPEC.md                     # 規格文件
└── GOOGLE_DRIVE_SETUP.md       # Google Drive 設定指南
```
