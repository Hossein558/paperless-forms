# 📋 سامانه دیجیتال فرم‌های بازرسی کیفیت (Paperless Forms)

![NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)
![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4?style=for-the-badge&logo=blazor)
![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?style=for-the-badge&logo=docker)
![Architecture](https://img.shields.io/badge/Architecture-Clean%20%2F%20Modular-green?style=for-the-badge)

سامانه **Paperless Forms** یک راهکار نرم‌افزاری پیشرفته و تحت وب جهت دیجیتال‌سازی فرم‌های بازرسی کیفیت در خطوط تولید (مانند فرم «بازرسی حین فرآیند تولید») در شرکت **کروز** است. این سیستم با حذف فرم‌های کاغذی، داده‌های بازرسی کیفیت، کنترل ابعادی، ظاهری، وزنی، نوع مواد و معیارهای پذیرش را به صورت آنلاین و پویا ثبت، اعتبارسنجی و نگهداری می‌نماید.

---

## 📌 فهرست مطالب
1. [ویژگی‌های کلیدی](#-ویژگیهای-کلیدی)
2. [معماری و لایه‌بندی پروژه](#-معماری-و-لایه‌بندی-پروژه)
3. [ساختار مخزن داده اکسل (Excel Database Engine)](#-ساختار-مخزن-داده-اکسل-excel-database-engine)
4. [احراز هویت یکپارچه (SSO) و ورود کاربران](#-احراز-هویت-یکپارچه-sso-و-ورود-کاربران)
5. [سرویس تصویر پروفایل (Avatar API) و بهینه‌سازی CIFS](#-سرویس-تصویر-پروفایل-avatar-api-و-بهینه‌سازی-cifs)
6. [زیرساخت داکر و Nginx Reverse Proxy](#-زیرساخت-داکر-و-nginx-reverse-proxy)
7. [راهنمای نصب، توسعه و استقرار](#-راهنمای-نصب-توسعه-و-استقرار)
8. [عیب‌یابی و لاگ‌ها (Troubleshooting)](#-عیب‌یابی-و-لاگ‌ها-troubleshooting)

---

## 🚀 ویژگی‌های کلیدی

- **طراحی فرانت‌اند پویا (Dynamic UI):** رندرینگ آنی فرم‌های بازرسی و پارامترهای کنترل کیفیت بر اساس متادیتای قطعه بدون نیاز به تغییر کد یا کمپایل مجدد.
- **یکپارچه‌سازی کامل با SSO سازمان:** احراز هویت متمرکز از طریق **Atlassian Crowd REST API** به همراه مکانیزم پشتیبان (Fallback) به **Active Directory (LDAP)**.
- **نمایش آواتار هوشمند کاربران:** سرو تصاویر پرسنلی از روی فایل‌شر شبکه (`CIFS/SMB`) با کارایی بسیار بالا (Direct File Stat بدون انومریشن برای بیش از ۵۵,۰۰۰ تصویر).
- **پشتیبانی از Nginx Reverse Proxy:** پیکربندی کامل Base Path روی زیرمسیر `/paperless/`.
- **موتور داده مبتنی بر Aspose.Cells:** بهره‌گیری از نسخه ۲۶.۶.۰ محلی Aspose.Cells با لایسنس معتبر `Aspose.Total` و مدیریت موازی‌سازی با قفل thread-safe.
- **مقاومت بالا در برابر داده‌های نامعتبر:** مکانیزم‌های `ParseDoubleSafe` و `ParseIntSafe` جهت جلوگیری از کرش سیستم در صورت وجود کاراکترهای غیرعددی (مانند `-` یا متون توضیحی در اکسل).
- **پایداری کلیدهای امنیتی (Data Protection):** ذخیره‌سازی متمرکز KeyRing در Shared Volume کانتینر جهت حفظ سشن‌ها در زمان ری‌استارت.

---

## 🏗️ معماری و لایه‌بندی پروژه

پروژه از الگوی **Clean Architecture / N-Tier** با فریم‌ورک **.NET 10** بهره می‌برد:

```text
Paperless-Forms/
├── Docs/                            # مستندات جامع معماری و استقرار
├── LocalDependencies/               # کتابخانه‌های محلی (Aspose.Cells.dll v26.6.0)
├── src/
│   ├── PaperlessForms.Core/         # لایه دامنه و منطق کسب‌وکار (Core Logic & Interfaces)
│   │   ├── Interfaces/              # اینترفیس‌های IPartRepository و IInspectionRepository
│   │   ├── Models/                  # مدل‌های Part, InspectionParameter, InspectionSubmission
│   │   └── Services/                # سرویس اصلی ExcelDataService, CrowdAuthService, ActiveDirectoryService
│   │
│   └── PaperlessForms.Web/          # لایه ارائه و کامپوننت‌های Blazor Server
│       ├── Components/              # کامپوننت‌ها و صفحات Blazor (InProcessInspection.razor, Login.razor)
│       ├── Controllers/             # APIهای سرویس‌دهنده (مانند Avatar API)
│       ├── Program.cs               # نقطه ورود برنامه و پیکربندی DI / BasePath / DataProtection
│       └── Dockerfile               # دستورالعمل ساخت ایمیج داکر
├── docker-compose.yml               # سرویس‌های کانتینری و مپینگ پورت/ولوم‌ها
└── PaperlessForms.sln               # سولوشن اصلی پروژه
```

---

## 📊 ساختار مخزن داده اکسل (Excel Database Engine)

در فاز جاری، متادیتای سیستم و ثبت‌های بازرسی در دو فایل اکسل ساختاریافته نگهداری می‌شوند:

### ۱. فایل `MasterData.xlsx`
* **شیت `Parts`:** نگهداری کد قطعه (`PartCode`)، نام قطعه، کد ایستگاه بازرسی، نام ایستگاه، کد ماشین و شماره برنامه کنترل (`ControlProgramNumber`).
* **شیت `Parameters`:** پارامترهای کنترلی هر قطعه شامل:
  - `RowNumber`: شماره ردیف پارامتر
  - `ParameterName`: نام ویژگی/پارامتر (مانند نوع مواد، ظاهری، وزن، ابعاد، مونتاژی)
  - `ParameterType`: نوع پارامتر (مواد، ظاهری، وزنی و...)
  - `AcceptanceCriteria`: معیار پذیرش
  - `ControlMethod`: روش کنترل (چشمی، ترازو، کولیس و...)
  - `MinValue` / `MaxValue`: حدود بالا و پایین تلرانس
  - `Unit`: واحد سنجش (gr, mm, ...)

### ۲. فایل `Submissions.xlsx`
* **شیت `Submissions`:** لاگ کامل فرم‌های ثبت‌شده توسط بازرسان کیفیت شامل شناسه یکتا (`Guid`)، داده‌های کامل بازرسی در قالب `JSON` پایش‌شده، کد قطعه، تاریخ و زمان ثبت، نام بازرس و وضعیت تایید.

---

## 🔐 احراز هویت یکپارچه (SSO) و ورود کاربران

احراز هویت در سامانه طی دو مرحله انجام پذیر است:
1. **Atlassian Crowd REST API (اصلی):** ارسال درخواست HTTP POST به `https://atlassian.crouseco.com/crowd/rest/usermanagement/1/authentication` با نام کاربری و کلمه عبور.
2. **Active Directory LDAP (پشتیبان):** در صورت عدم پاسخگویی Crowd، سامانه به صورت اتوماتیک به سرویس LDAP دامین سازمان سوییچ کرده و کاربر را صحه‌گذاری می‌نماید.
3. **کوکی سشن (Authentication Cookie):** پس از ورود موفق، Claims شامل نام کاربری و نام کامل فارسی (`displayName`) در کوکی رمزنگاری‌شده مرورگر ذخیره می‌شود.

---

## 🖼️ سرویس تصویر پروفایل (Avatar API) و بهینه‌سازی CIFS

- **Endpoint اختصاصی:** `/api/avatar`
- **چالش:** وجود بیش از ۵۵,۰۰۰ تصویر پرسنلی روی شبکه CIFS SMB (`//datap2/Crouse/...`) که موجب کندی شدید در انومریشن فایل‌ها می‌شد.
- **راهکار:** 
  - لغو کامل `Directory.EnumerateFiles`.
  - استفاده از متد مستقیم `File.Exists` با تست ترکیب‌های رایج پسوند (`.jpg`, `.JPG`, `.jpeg`) و کد پرسنلی (با/بدون صفر ابتدایی مانند `1-110749.jpg` و `1-0016.JPG`).
  - این راهکار زمان پاسخگویی API را از بیش از ۳۰ ثانیه به **کمتر از ۵ میلی‌ثانیه** کاهش داد.

---

## 🐳 زیرساخت داکر و Nginx Reverse Proxy

### تنظیمات Docker Compose (`docker-compose.yml`)
```yaml
version: '3.8'
services:
  paperless-forms:
    image: paperless-forms:latest
    container_name: paperless-forms
    user: "root" # دسترسی کامل به شبکه CIFS Mount
    ports:
      - "8081:8080" # مپ پورت ۸۰۸۱ میزبان به ۸۰۸۰ کانتینر
    volumes:
      - /mnt/sharedhome/paperless forms/Data:/app/Data
      - /mnt/images:/app/Avatars # مسیر مپ‌شده تصاویر پرسنلی
      - /mnt/sharedhome/paperless forms/Keys:/app/Keys # پایداری Data Protection
    restart: always
```

### پیکربندی Subpath Nginx
برنامه طوری تنظیم شده است که پشت Nginx Reverse Proxy تحت زیرمسیر `/paperless/` اجرا شود:
- در `Program.cs`: `app.UsePathBase("/paperless");`
- در `App.razor`: `<base href="/paperless/" />`

---

## 🛠️ راهنمای نصب، توسعه و استقرار

### پیش‌نیازها
- **.NET 10.0 SDK**
- **Docker & Docker Compose** روی سرورهای لینوکس (Node 1: `172.25.0.42` / Node 2: `172.25.0.43`)

### ۱. کامپایل و اجرای محلی
```bash
# ساخت پروژه
dotnet build

# اجرای برنامه روی سیستم محلی
cd src/PaperlessForms.Web
dotnet run
```

### ۲. انتشار و ساخت ایمیج داکر
```bash
# پابلیک کردن خروجی
dotnet publish src/PaperlessForms.Web/PaperlessForms.Web.csproj -c Release -o C:\temp\publish

# ساخت ایمیج داکر
docker build -t paperless-forms:latest -f src/PaperlessForms.Web/Dockerfile .
```

### ۳. استقرار روی سرور عملیاتی (Node 1 & Node 2)
1. ذخیره ایمیج در قالب tar:
   `docker save -o paperless-forms.tar paperless-forms:latest`
2. انتقال فایل `.tar` به مسیر مشترک `/mnt/sharedhome/paperless forms/`.
3. اجرا روی سرور هدف via SSH:
   ```bash
   cd "/mnt/sharedhome/paperless forms"
   docker load -i paperless-forms.tar
   docker compose down
   docker compose up -d
   ```

---

## 🔍 عیب‌یابی و لاگ‌ها (Troubleshooting)

### ۱. مشاهده لاگ‌های کانتینر روی سرور
```bash
docker logs paperless-forms --tail 100 -f
```

### ۲. خطای اشغال پورت 8081
در صورت بروز خطای `bind: address already in use` برای پورت 8081:
```bash
# شناسایی پروسس اشغال‌کننده
ss -tulpn | grep :8081

# متوقف کردن پروسس مزاحم (مثلاً PID 1111)
kill -9 <PID>

# استارت مجدد سرویس
docker compose up -d
```

---

### 👨‍💻 نگهداری و توسعه‌دهندگان
* **توسعه‌دهنده:** حسین ابراهیمی (واحد نرم‌افزار و سیستم‌ها - شرکت کروز)
* **تکنولوژی‌ها:** .NET 10, Blazor Server, Aspose.Cells, Atlassian Crowd, Docker, Nginx.
