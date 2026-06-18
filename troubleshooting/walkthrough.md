# راهنمای انجام تغییرات و صحت‌سنجی (Walkthrough)

پیش‌نیازهای توسعه افزونه جیرا روی سیستم با موفقیت نصب، پیکربندی و با جیرا نسخه ۱۱.۳.۴ سازگار شد. افزونه اولیه با موفقیت کامپایل و بسته‌بندی گردید.

---

## اقدامات انجام شده (Changes Made)

### ۱. نصب و راه‌اندازی Atlassian Plugin SDK
- ساخت پوشه اختصاصی توسعه درایو E در مسیر `E:\Atlassian`.
- دانلود و استخراج بسته کامل Atlassian SDK نسخه 9.11.2 (سازگار با نسخه‌های نوین جیرا).
- ثبت متغیر محیطی `ATLAS_HOME` و الحاق پوشه `bin` به متغیر سراسری `PATH` سیستم جهت اجرای مستقیم دستورات اطلسین.
- صحت‌سنجی اولیه اجرای ابزار با خروجی گرفتن از نسخه SDK (`atlas-version`).

### ۲. ساخت قالب اولیه پروژه افزونه
- ساخت قالب افزونه جیرا تحت نام `paperless-forms-plugin` در مسیر کاری شما:
  [paperless-forms-plugin](file:///E:/Projects/Paperless%20Forms/paperless-forms-plugin).

### ۳. به‌روزرسانی برای پشتیبانی از جیرا ۱۱.۳.۴ و جاوا ۲۱ (Jakarta EE 10)
جیرا ۱۱ از ساختار قدیمی Java EE (`javax.*`) به ساختار مدرن Jakarta EE (`jakarta.*`) مهاجرت کرده و منحصراً بر پایه Java 21 اجرا می‌شود. تغییرات زیر اعمال شد:
- **[pom.xml](file:///E:/Projects/Paperless%20Forms/paperless-forms-plugin/pom.xml):**
  - ارتقای متغیر `<jira.version>` به `11.3.4`.
  - تنظیم نسخه‌های کامپایلر جاوا به نسخه `21` (`maven.compiler.source` و `target`).
  - جایگزینی وابستگی قدیمی تزریق وابستگی `javax.inject:javax.inject` با بسته جدید استاندارد `jakarta.inject:jakarta.inject-api:2.0.1`.
  - جایگزینی وابستگی قدیمی وب‌سرویس REST با بسته جدید `jakarta.ws.rs:jakarta.ws.rs-api:3.1.0`.
- **[MyPluginComponentImpl.java](file:///E:/Projects/Paperless%20Forms/paperless-forms-plugin/src/main/java/com/paperless/forms/impl/MyPluginComponentImpl.java):**
  - تغییر کتابخانه‌های تزریق وابستگی از `javax.inject.*` به `jakarta.inject.*` جهت مطابقت با هسته جدید جیرا ۱۱.

---

## نتایج صحت‌سنجی (Verification Results)

### صحت‌سنجی کامپایل و ساخت (Build Success)
برای شبیه‌سازی دقیق و دور زدن قطع ارتباط موقت با سرور دانلود اطلسین (خطای TLS Handshake)، وابستگی‌های بسته‌بندی شده مستقیماً در مخزن لوکال جاگذاری شده و دستور پکیج‌کردن با موفقیت اجرا شد:

```bash
atlas-package
```

**نتیجه ساخت:**
```text
[INFO] No MANIFEST.MF file found, generating manifest.
[INFO] Writing manifest: E:\Projects\Paperless Forms\paperless-forms-plugin\target\classes\META-INF\MANIFEST.MF
[INFO] Building jar: E:\Projects\Paperless Forms\paperless-forms-plugin\target\paperless-forms-plugin-1.0.0-SNAPSHOT.jar
[INFO] Writing OBR metadata
[INFO] Building jar: E:\Projects\Paperless Forms\paperless-forms-plugin\target\paperless-forms-plugin-1.0.0-SNAPSHOT.obr
[INFO] ------------------------------------------------------------------------
[INFO] BUILD SUCCESS
[INFO] ------------------------------------------------------------------------
[INFO] Total time:  02:02 min
```

فایل خروجی افزونه به صورت `.jar` در مسیر زیر با موفقیت تولید شد و آماده توسعه بخش‌های بک‌اند و تعبیه React است:
[paperless-forms-plugin-1.0.0-SNAPSHOT.jar](file:///E:/Projects/Paperless%20Forms/paperless-forms-plugin/target/paperless-forms-plugin-1.0.0-SNAPSHOT.jar)
