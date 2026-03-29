# Cashew Transaction API — 功能規格與技術規劃

## 1. 專案概述

### 1.1 目標
建立一個 .NET Web API，透過 Google Drive 存取 Cashew App 的 SQLite 備份檔案，提供交易 (Transaction) 的新增與刪除功能，讓使用者可以透過 API 完成記帳操作。

### 1.2 架構概念
```
Client → .NET Web API → Google Drive API → SQLite File (download/upload)
                             ↕
                        Local SQLite (臨時操作)
```

**流程：**
1. API 從 Google Drive 下載 Cashew 的 SQLite 備份檔 (`.sql`)
2. 在本地開啟 SQLite 資料庫執行 CRUD 操作
3. 操作完成後，將更新後的 SQLite 檔案上傳回 Google Drive
4. 使用者在 Cashew App 中匯入更新後的備份檔

---

## 2. 功能規格 (Functional Spec)

### 2.1 核心功能

#### F1: 交易查詢
- **GET** `/api/transactions` — 查詢所有交易，支援分頁、篩選
- **GET** `/api/transactions/{pk}` — 查詢單筆交易

#### F2: 新增交易
- **POST** `/api/transactions` — 新增一筆交易
- **POST** `/api/transactions/batch` — 批次新增多筆交易

#### F3: 刪除交易
- **DELETE** `/api/transactions/{pk}` — 刪除一筆交易（含刪除日誌記錄）
- **POST** `/api/transactions/batch-delete` — 批次刪除多筆交易

#### F4: 輔助查詢
- **GET** `/api/categories` — 查詢所有分類（新增交易時需要選擇分類）
- **GET** `/api/wallets` — 查詢所有錢包帳戶

#### F5: Google Drive 同步
- **POST** `/api/sync/pull` — 從 Google Drive 下載最新的 SQLite 資料庫
- **POST** `/api/sync/push` — 將本地修改後的 SQLite 上傳回 Google Drive
- **GET** `/api/sync/status` — 取得目前同步狀態

### 2.2 資料模型

#### Transaction（交易）— 必填欄位
| 欄位 | 類型 | 必填 | 說明 |
|------|------|------|------|
| `name` | string | ✅ | 交易名稱（最多 250 字元）|
| `amount` | decimal | ✅ | 金額（正數，系統自動處理正負號）|
| `categoryFk` | string | ✅ | 分類 FK（必須是已存在的分類）|
| `income` | bool | ✅ | true=收入, false=支出 |

#### Transaction — 選填欄位
| 欄位 | 類型 | 預設值 | 說明 |
|------|------|--------|------|
| `note` | string | `""` | 備註（最多 500 字元）|
| `walletFk` | string | `"0"` | 錢包 FK |
| `dateCreated` | datetime | `now` | 交易日期 |
| `subCategoryFk` | string? | `null` | 子分類 FK |
| `type` | int? | `null` | 特殊類型(0=upcoming,1=subscription...) |
| `paid` | bool | `false` | 是否已付款 |
| `objectiveFk` | string? | `null` | 儲蓄目標 FK |

### 2.3 業務規則

1. **金額限制**：金額必須在 ±999,999,999,999 範圍內
2. **分類驗證**：`categoryFk` 必須指向存在的分類記錄
3. **錢包驗證**：`walletFk` 必須指向存在的錢包記錄
4. **刪除日誌**：刪除交易時必須在 `delete_logs` 表中記錄，以保持與 Cashew App 同步相容性
5. **主鍵生成**：交易 PK 使用 UUID v4 格式（`uuid.v4()`）
6. **日期格式**：所有日期以 UTC 時間處理，存儲為 Unix timestamp（毫秒）
7. **子分類驗證**：如果提供 `subCategoryFk`，該分類的 `mainCategoryPk` 必須等於 `categoryFk`

---

## 3. 注意事項

### 3.1 資料一致性
- ⚠️ **並發問題**：SQLite 檔案基於 Google Drive 的下載/上傳機制，不支援多人同時操作。API 應實作鎖機制（Mutex/Semaphore）確保同一時間只有一個操作在修改資料庫。
- ⚠️ **同步衝突**：如果使用者在 Cashew App 與 API 之間切換操作，可能會有資料衝突。建議操作流程為：Pull → 操作 → Push → App 匯入。

### 3.2 Google Drive 整合
- 使用 Google Drive API v3
- 需要 OAuth 2.0 Service Account 或 User credentials
- SQLite 檔案在 Google Drive 上的識別方式：透過檔名 `cashew-*.sql` 或指定的 file ID
- 需處理 Google Drive API 的 rate limit 與 quota

### 3.3 SQLite 相容性
- Cashew 使用 Drift (原 moor) 生成的 SQLite 資料庫，schema version = 46
- 必須確保 .NET 端讀寫的 SQLite 與 Cashew App 完全相容
- 日期存儲格式：Unix timestamp (milliseconds since epoch)
- Boolean 存儲：0/1 (SQLite 沒有原生 boolean)

### 3.4 安全性
- Google Drive credentials 不應硬編碼，應使用環境變數或 Secret Manager
- API 應支援 API Key 或 Bearer Token 驗證
- 限制上傳/下載的檔案大小

---

## 4. 技術規劃

### 4.1 技術選型
| 項目 | 選型 | 說明 |
|------|------|------|
| 框架 | ASP.NET Core 8.0 Minimal API | 輕量、高效 |
| SQLite | Microsoft.Data.Sqlite + EF Core SQLite | 讀寫 Cashew SQLite |
| Google Drive | Google.Apis.Drive.v3 | Google Drive 檔案操作 |
| 測試 | xUnit + Moq | 單元測試 |
| 文件 | Swagger / OpenAPI | API 文件自動產生 |

### 4.2 專案結構
```
DotnetTransactionAPI/
├── CashewAPI/
│   ├── Program.cs                    # 應用程式進入點
│   ├── appsettings.json              # 設定檔
│   ├── Models/
│   │   ├── Transaction.cs            # 交易模型
│   │   ├── Category.cs               # 分類模型
│   │   ├── Wallet.cs                 # 錢包模型
│   │   ├── DeleteLog.cs              # 刪除日誌模型
│   │   └── ApiModels/
│   │       ├── CreateTransactionRequest.cs
│   │       ├── TransactionResponse.cs
│   │       └── SyncStatusResponse.cs
│   ├── Services/
│   │   ├── ICashewDatabase.cs        # 資料庫服務介面
│   │   ├── CashewDatabase.cs         # SQLite 操作實作
│   │   ├── IGoogleDriveService.cs    # Google Drive 介面
│   │   └── GoogleDriveService.cs     # Google Drive 實作
│   ├── Endpoints/
│   │   ├── TransactionEndpoints.cs   # 交易 API 端點
│   │   ├── CategoryEndpoints.cs      # 分類 API 端點
│   │   ├── WalletEndpoints.cs        # 錢包 API 端點
│   │   └── SyncEndpoints.cs          # 同步 API 端點
│   └── Middleware/
│       └── ApiKeyMiddleware.cs       # API Key 驗證
├── CashewAPI.Tests/
│   ├── Services/
│   │   └── CashewDatabaseTests.cs
│   └── Endpoints/
│       └── TransactionEndpointsTests.cs
├── SPEC.md                           # 本文件
└── CashewAPI.sln                     # Solution 檔案
```

### 4.3 API 端點詳細設計

#### 4.3.1 GET /api/transactions
```
Query Parameters:
  - page (int, default: 1)
  - pageSize (int, default: 20, max: 100)
  - walletFk (string, optional) — 篩選特定錢包
  - categoryFk (string, optional) — 篩選特定分類
  - startDate (datetime, optional) — 起始日期
  - endDate (datetime, optional) — 結束日期
  - income (bool, optional) — 篩選收入/支出

Response: 200 OK
{
  "data": [...],
  "page": 1,
  "pageSize": 20,
  "totalCount": 150
}
```

#### 4.3.2 POST /api/transactions
```
Request Body:
{
  "name": "午餐",
  "amount": 150.00,
  "categoryFk": "uuid-of-food-category",
  "income": false,
  "note": "公司附近便當",
  "walletFk": "0",
  "dateCreated": "2026-03-29T12:00:00Z"
}

Response: 201 Created
{
  "transactionPk": "generated-uuid",
  ...
}
```

#### 4.3.3 DELETE /api/transactions/{pk}
```
Response: 200 OK
{
  "message": "Transaction deleted",
  "deleteLogPk": "generated-uuid"
}
```

#### 4.3.4 POST /api/sync/pull
```
Request Body:
{
  "fileId": "google-drive-file-id"    // 可選，若不提供則搜尋最新的 cashew-*.sql
}

Response: 200 OK
{
  "message": "Database synced from Google Drive",
  "fileName": "cashew-2026-03-29-120000.sql",
  "transactionCount": 1250
}
```

#### 4.3.5 POST /api/sync/push
```
Response: 200 OK
{
  "message": "Database uploaded to Google Drive",
  "fileId": "google-drive-file-id",
  "fileName": "cashew-2026-03-29-130000.sql"
}
```

### 4.4 資料庫存取策略

1. **下載階段**：從 Google Drive 下載 `.sql` 檔案至本地暫存目錄
2. **操作階段**：使用 `Microsoft.Data.Sqlite` 直接讀寫 SQLite 檔案
3. **上傳階段**：操作完成後上傳回 Google Drive

```csharp
// 虛擬碼
var dbBytes = await googleDrive.DownloadFile(fileId);
File.WriteAllBytes(localDbPath, dbBytes);

using var connection = new SqliteConnection($"Data Source={localDbPath}");
// 執行 CRUD 操作...

var updatedBytes = File.ReadAllBytes(localDbPath);
await googleDrive.UploadFile(updatedBytes, fileName);
```

### 4.5 錯誤處理

| HTTP Code | 使用場景 |
|-----------|----------|
| 200 | 成功 |
| 201 | 新增成功 |
| 400 | 參數驗證失敗、金額超出範圍 |
| 404 | 交易/分類/錢包不存在 |
| 409 | 同步衝突（資料庫被鎖定）|
| 500 | Google Drive API 錯誤、SQLite 錯誤 |
| 503 | 資料庫未同步（需先執行 pull）|

### 4.6 開發階段規劃

| 階段 | 內容 | 估計時間 |
|------|------|----------|
| Phase 1 | 專案骨架 + SQLite 讀寫 + Transaction CRUD | 核心 |
| Phase 2 | Google Drive 整合 | 核心 |
| Phase 3 | 輔助端點（Categories, Wallets）| 次要 |
| Phase 4 | 驗證 + 中介層 + 錯誤處理 | 重要 |
| Phase 5 | 單元測試 + 整合測試 | 重要 |
