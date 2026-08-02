# 📚 راهنمای جامع معماری، توسعه و نگهداری سامانه فرم‌های دیجیتال (Paperless Forms)

**تاریخ آخرین بروزرسانی:** مرداد ۱۴۰۵ (August 2026)  
**نسخه:** ۲.۰ (تولید با .NET 10 & Blazor Server)  
**سازمان:** شرکت صنایع تولیدی کروز - واحد سیستم‌ها و نرم‌افزار  

---

## 📑 جدول مندرجات
1. [مقدمه و اهداف پروژه](#1-مقدمه-و-اهداف-پروژه)
2. [معماری کامل سیستم (System Architecture)](#2-معماری-کامل-سیستم-system-architecture)
3. [ساختار داده‌ها و مدل‌های کسب‌وکار](#3-ساختار-داده‌ها-و-مدل‌های-کسب‌وکار)
4. [مکانیزم احراز هویت و یکپارچه‌سازی با Crowd / AD](#4-مکانیزم-احراز-هویت-و-یکپارچه‌سازی-با-crowd--ad)
5. [سرویس دریافت تصویر آواتار و بهینه‌سازی شبکه CIFS](#5-سرویس-دریافت-تصویر-آواتار-و-بهینه‌سازی-شبکه-cifs)
6. [مدیریت فایل‌های اکسل با Aspose.Cells و کنترل Concurrency](#6-مدیریت-فایل‌های-اکسل-با-asposecells-و-کنترل-concurrency)
7. [تنظیمات داکر، شبکه و Nginx Reverse Proxy](#7-تنظیمات-داکر-شبکه-و-nginx-reverse-proxy)
8. [راهنمای خطایابی و عیب‌یابی (Troubleshooting Checklist)](#8-راهنمای-خطایابی-و-عیب‌یابی-troubleshooting-checklist)

---

## ۱. مقدمه و اهداف پروژه

سامانه **Paperless Forms** جهت حذف فرم‌های کاغذی بازرسی کیفیت در خطوط تولید شرکت کروز (شامل فرم‌های بازرسی حین فرآیند، کنترل کیفی مواد اولیه، تست‌های ابعادی و وزنی) طراحی شده است.

### اهداف اصلی:
- **پویایی کامل فرم‌ها (Dynamic Forms):** عدم هاردکد کردن پارامترهای بازرسی؛ فرم‌ها از روی فایل اکسل متادیتا خوانی شده و رندر می‌شوند.
- **یکپارچگی با سیستم‌های سازمانی:** ورود کاربران بر اساس حساب کاربری دامین (Atlassian Crowd / Active Directory).
- **عملکرد مستقل و بالا:** عدم وابستگی به Jira جهت بالا بردن سرعت ورود داده‌ها و جلوگیری از محدودیت‌های لایسنس و رابط کاربری.

---

## ۲. معماری کامل سیستم (System Architecture)

### الگوی معماری:
پروژه بر اساس الگوی **Modular Monolith** و اصالت **Clean Architecture** پیاده‌سازی شده است:

```text
                               +----------------------------------+
                               |     مرورگر کاربر (Client UI)     |
                               +----------------------------------+
                                                |
                                                v (HTTPS / Nginx)
                               +----------------------------------+
                               |     Nginx Reverse Proxy (/paperless)
                               +----------------------------------+
                                                |
                                                v (Port 8081)
                               +----------------------------------+
                               |   Blazor Server App (.NET 10)    |
                               |  - SignalR Circuit Management    |
                               |  - InProcessInspection.razor     |
                               |  - AvatarController (/api/avatar)|
                               +----------------------------------+
                                 /              |               \
                                /               |                \
                               v                v                 v
           +---------------------+    +--------------------+    +--------------------+
           | Atlassian Crowd API |    |  Active Directory  |    | Network File Share |
           |  (SSO Primary Auth) |    |  (LDAP Fallback)   |    | (CIFS /mnt/images) |
           +---------------------+    +--------------------+    +--------------------+
                                                |
                                                v
                                  +----------------------------+
                                  | Aspose.Cells 26.6.0 Engine |
                                  | - MasterData.xlsx          |
                                  | - Submissions.xlsx         |
                                  +----------------------------+
```

---

## ۳. ساختار داده‌ها و مدل‌های کسب‌وکار

مدل‌های اصلی در پروژه `PaperlessForms.Core` قرار دارند:

### ۱. مدل `Part` (`Part.cs`)
نماینده اطلاعات پایه قطعه و ایستگاه بازرسی:
- `PartCode` (کد قطعه)
- `PartName` (نام قطعه / محصول)
- `InspectionStationCode` / `InspectionStationName` (کد و نام ایستگاه بازرسی)
- `MachineCode` (کد ماشین تولیدی)
- `ControlProgramNumber` (شماره برنامه کنترل / Control Plan)
- `Parameters` (لیست پارامترهای بازرسی وابسته)

### ۲. مدل `InspectionParameter` (`InspectionParameter.cs`)
نماینده یک ردیف پارامتر کنترلی:
- `RowNumber` (شماره ردیف)
- `ParameterName` (نام پارامتر - مثلاً وزن قطعه، نوع مواد، ظاهری)
- `ParameterType` (نوع پارامتر: وزنی، ظاهری، ابعادی، مواد)
- `AcceptanceCriteria` (معیار پذیرش)
- `ControlMethod` (روش کنترل: چشمی، ترازو، ابزار سنجش)
- `MinValue` / `MaxValue` (حدود تلرانس پایین و بالا)
- `Unit` (واحد اندازه گیری: gr, mm, ...)

### ۳. مدل `InspectionSubmission` (`InspectionSubmission.cs`)
ثبت نهایی فرم بازرسی:
- `Id` (شناسه یکتا Guid)
- `PartCode` (کد قطعه)
- `SubmittedAt` (تاریخ و زمان ثبت)
- `InspectorName` (نام و نام خانوادگی بازرس)
- `Status` (وضعیت تایید)
- `SampleResults` (نتایج نمونه‌های اندازه گیری شده)

---

## ۴. مکانیزم احراز هویت و یکپارچه‌سازی با Crowd / AD

احراز هویت کاربران توسط کلاس `CrowdAuthService.cs` انجام می‌شود:

1. **درخواست لاگین:** کاربر نام کاربری (مثلاً `he110749`) و کلمه عبور را وارد می‌کند.
2. **استعلام از Crowd:** سیستم ابتدا متد `AuthenticateAsync` را فرامی‌خواند و درخواست HTTP POST به آدرس `https://atlassian.crouseco.com/crowd/rest/usermanagement/1/authentication` ارسال می‌کند.
3. **دریافت اطلاعات کاربر:** پس از تایید رمز، اطلاعات نام کامل فارسی (`displayName`) کاربر فرخوانده می‌شود.
4. **مکانیزم Fallback (LDAP):** در صورتی که سرویس Crowd قطع باشد یا خطای شبکه رخ دهد، سیستم اتوماتیک متد `AuthenticateViaLdap` در `ActiveDirectoryService.cs` را صدا زده و با دامین `crouseco.com` احراز هویت را انجام می‌دهد.

---

## ۵. سرویس دریافت تصویر آواتار و بهینه‌سازی شبکه CIFS

یکی از چالش‌های اصلی سیستم، بارگذاری عکس‌های پرسنلی از روی فایل‌شر شبکه (`//datap2/Crouse/Services-Support-P2/Personel`) بود که روی سرور لینوکس در `/mnt/images` Mount شده است.

### مشکل قبلی:
استفاده از `Directory.EnumerateFiles` روی شبکه CIFS با ۵۵,۰۰۰ تصویر باعث ایجاد کندی تا ۳۰ ثانیه و Timeout شدن مرورگر می‌شد.

### راهکار بهینه‌سازی:
1. عدم لیست کردن فایل‌های دایرکتوری شبکه.
2. ساخت کد پرسنلی غیراستاندارد (پشتیبانی از کدهای بدون صفر و با صفر ابتدایی).
3. انجام مستقیم متد `File.Exists` برای پسوندهای `.jpg` و `.JPG`.
4. مپ کردن Endpoint به `/api/avatar` در `Program.cs`.

---

## ۶. مدیریت فایل‌های اکسل با Aspose.Cells و کنترل Concurrency

### ۱. استفاده از `MaxDataRow`
در نسخه جدید جهت جلوگیری از توقف روال خواندن اکسل هنگام برخورد با سطرهای خالی ناخواسته، کدهای اسکن شیت‌ها از الگوی زیر استفاده می‌کنند:
```csharp
int maxRow = sheet.Cells.MaxDataRow;
for (int row = 1; row <= maxRow; row++)
{
    var cellValue = sheet.Cells[row, 0].StringValue;
    if (string.IsNullOrWhiteSpace(cellValue)) continue;
    // ...
}
```

### ۲. اعتبارسنجی ایمن مقادیر عددی (`ParseDoubleSafe`)
جهت جلوگیری از استثنای `Aspose.Cells.CellsException: Not a numeric value` در صورت وجود متون غیرعددی یا خط تیره (`-`) در ستون‌های Min/Max:
```csharp
private double? ParseDoubleSafe(Aspose.Cells.Cell cell)
{
    var str = cell.StringValue?.Trim();
    if (string.IsNullOrWhiteSpace(str) || str == "-") return null;
    if (double.TryParse(str, out double val)) return val;
    return null;
}
```

### ۳. مدیریت Concurrency با `SemaphoreSlim`
هنگام نوشتن در فایل `Submissions.xlsx` جهت جلوگیری از تداخل ناشی از ثبت همزمان بازرسان، از قفل تک ورودی `SemaphoreSlim _writeLock = new(1, 1);` استفاده شده است.

---

## ۷. تنظیمات داکر، شبکه و Nginx Reverse Proxy

### ۱. دسترسی Root در کانتینر
به دلیل دسترسی‌های فایل‌شر شبکه لینوکس، کانتینر در `docker-compose.yml` با دسترسی `user: "root"` اجرا می‌شود.

### ۲. پایداری کلیدهای DataProtection
کلیدهای متغیر رمزنگاری در مسیر `/app/Keys` ذخیره شده و به یک ولوم روی سرور مپ می‌شوند تا سشن‌های لاگین کاربران بعد از ری‌استارت کانتینر معتبر باقی بمانند.

### ۳. تنظیمات Nginx Subpath
برنامه در `Program.cs` با `app.UsePathBase("/paperless");` پیکربندی شده است.

---

## ۸. راهنمای خطایابی و عیب‌یابی (Troubleshooting Checklist)

| مشکـل | علت احتمالی | راهکار رفع |
|---|---|---|
| **خطای 404 در عکس آواتار** | عدم وجود عکس یا عدم دسترسی روت به CIFS | بررسی mount بودن `/mnt/images` و لاگ `docker logs paperless-forms` |
| **خطای 401 در ورود** | اشتباه بودن رمز یا قطع ارتباط Crowd/LDAP | بررسی لاگ سرویس Crowd و تست دسترسی شبکه از نود به اتلاسیان |
| **اشغال بودن پورت 8081** | اجرای پروسس موازات رو سرور | اجرای `ss -tulpn \| grep :8081` و `kill -9 <PID>` |
| **عدم نمایش برخی پارامترها** | وجود سطر خالی یا متن غیرعددی در اکسل | بررسی کدهای `ParseDoubleSafe` و صحت شیت `Parameters` |
