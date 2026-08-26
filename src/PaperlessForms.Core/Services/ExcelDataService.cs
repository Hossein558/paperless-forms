using Aspose.Cells;
using PaperlessForms.Core.Interfaces;
using PaperlessForms.Core.Models;
using System.Text.Json;

namespace PaperlessForms.Core.Services;

/// <summary>
/// سرویس اصلی برای خواندن و نوشتن داده‌ها از/به فایل اکسل
/// </summary>
public class ExcelDataService : IPartRepository, IInspectionRepository
{
    private readonly string _masterDataPath;
    private readonly string _submissionsPath;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    // نام شیت‌ها
    private const string SheetParts = "Parts";
    private const string SheetParameters = "Parameters";
    private const string SheetSubmissions = "Submissions";

    public ExcelDataService(string baseFolderPath)
    {
        _masterDataPath = Path.Combine(baseFolderPath, "MasterData.xlsx");
        _submissionsPath = Path.Combine(baseFolderPath, "Submissions.xlsx");

        EnsureFilesExist();
    }

    // ─────────────────────────────────────────────
    //   IPartRepository
    // ─────────────────────────────────────────────

    public Task<List<Part>> GetAllPartsAsync()
    {
        return Task.Run(() =>
        {
            var parts = new List<Part>();
            var workbook = new Workbook(_masterDataPath);
            var sheet = workbook.Worksheets[SheetParts];
            int row = 1; // سطر ۰ هدر است

            while (!string.IsNullOrWhiteSpace(sheet.Cells[row, 0].StringValue))
            {
                parts.Add(MapRowToPart(sheet, row));
                row++;
            }
            return parts;
        });
    }

    public Task<Part?> GetPartByCodeAsync(string partCode)
    {
        return Task.Run(() =>
        {
            var workbook = new Workbook(_masterDataPath);
            var sheet = workbook.Worksheets[SheetParts];
            int maxRow = sheet.Cells.MaxDataRow;
            
            Console.WriteLine($"[Diagnostic] GetPartByCodeAsync called for: '{partCode}'. MaxDataRow: {maxRow}");

            for (int row = 1; row <= maxRow; row++)
            {
                var cellValue = sheet.Cells[row, 0].StringValue;
                if (string.IsNullOrWhiteSpace(cellValue)) continue;

                if (cellValue.Trim().Equals(partCode.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    var part = MapRowToPart(sheet, row);
                    part.Parameters = GetParametersForPart(partCode);
                    return part;
                }
            }
            
            Console.WriteLine($"[Diagnostic] PartCode '{partCode}' NOT FOUND in Parts sheet.");
            return (Part?)null;
        });
    }

    private static Part MapRowToPart(Worksheet sheet, int row) => new()
    {
        PartCode = sheet.Cells[row, 0].StringValue.Trim(),
        PartName = sheet.Cells[row, 1].StringValue.Trim(),
        InspectionStationCode = sheet.Cells[row, 2].StringValue.Trim(),
        InspectionStationName = sheet.Cells[row, 3].StringValue.Trim(),
        MachineCode = sheet.Cells[row, 4].StringValue.Trim(),
        ControlProgramNumber = sheet.Cells[row, 5].StringValue.Trim(),
    };

    private double? ParseDoubleSafe(Aspose.Cells.Cell cell)
    {
        var str = cell.StringValue?.Trim();
        if (string.IsNullOrWhiteSpace(str) || str == "-") return null;
        if (double.TryParse(str, out double val)) return val;
        return null;
    }

    private int ParseIntSafe(Aspose.Cells.Cell cell)
    {
        var str = cell.StringValue?.Trim();
        if (string.IsNullOrWhiteSpace(str)) return 0;
        if (int.TryParse(str, out int val)) return val;
        if (double.TryParse(str, out double dVal)) return (int)dVal;
        return 0;
    }

    private List<InspectionParameter> GetParametersForPart(string partCode)
    {
        var parameters = new List<InspectionParameter>();
        var workbook = new Workbook(_masterDataPath);
        var sheet = workbook.Worksheets[SheetParameters];
        int maxRow = sheet.Cells.MaxDataRow;

        Console.WriteLine($"[Diagnostic] GetParametersForPart called for partCode: '{partCode}'");
        Console.WriteLine($"[Diagnostic] MaxDataRow in Parameters sheet: {maxRow}");

        for (int row = 1; row <= maxRow; row++)
        {
            var cellValue = sheet.Cells[row, 0].StringValue;
            if (string.IsNullOrWhiteSpace(cellValue)) continue;

            // Console.WriteLine($"[Diagnostic] Row {row} PartCode: '{cellValue}'"); // Uncomment if needed, but might be noisy

            if (cellValue.Trim().Equals(partCode.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                parameters.Add(new InspectionParameter
                {
                    RowNumber = ParseIntSafe(sheet.Cells[row, 1]),
                    ParameterName = sheet.Cells[row, 2].StringValue?.Trim(),
                    ParameterType = sheet.Cells[row, 3].StringValue?.Trim(),
                    AcceptanceCriteria = sheet.Cells[row, 4].StringValue?.Trim(),
                    ControlMethod = sheet.Cells[row, 5].StringValue?.Trim(),
                    MinValue = ParseDoubleSafe(sheet.Cells[row, 6]),
                    MaxValue = ParseDoubleSafe(sheet.Cells[row, 7]),
                    Unit = sheet.Cells[row, 8].StringValue?.Trim(),
                });
            }
        }
        
        Console.WriteLine($"[Diagnostic] Found {parameters.Count} parameters for '{partCode}'");
        return parameters;
    }

    // ─────────────────────────────────────────────
    //   IInspectionRepository
    // ─────────────────────────────────────────────

    public Task<List<InspectionSubmission>> GetSubmissionsAsync(string? partCode = null, DateTime? from = null, DateTime? to = null)
    {
        return Task.Run(() =>
        {
            var results = new List<InspectionSubmission>();
            if (!File.Exists(_submissionsPath)) return results;

            var workbook = new Workbook(_submissionsPath);
            var sheet = workbook.Worksheets[SheetSubmissions];
            int row = 1;

            while (!string.IsNullOrWhiteSpace(sheet.Cells[row, 0].StringValue))
            {
                var jsonData = sheet.Cells[row, 1].StringValue;
                if (!string.IsNullOrWhiteSpace(jsonData))
                {
                    var submission = JsonSerializer.Deserialize<InspectionSubmission>(jsonData);
                    if (submission != null)
                    {
                        // فیلتر
                        if (partCode != null && !submission.PartCode.Equals(partCode, StringComparison.OrdinalIgnoreCase)) { row++; continue; }
                        if (from.HasValue && submission.SubmittedAt < from.Value) { row++; continue; }
                        if (to.HasValue && submission.SubmittedAt > to.Value) { row++; continue; }
                        results.Add(submission);
                    }
                }
                row++;
            }
            return results;
        });
    }

    public Task<InspectionSubmission?> GetByIdAsync(Guid id)
    {
        return Task.Run(async () =>
        {
            var all = await GetSubmissionsAsync();
            return all.FirstOrDefault(s => s.Id == id);
        });
    }

    public async Task<List<DecomposedInspectionRecord>> GetDecomposedSubmissionsAsync(InspectionReportFilter? filter = null)
    {
        var submissions = await GetSubmissionsAsync(filter?.PartCode, filter?.FromDate, filter?.ToDate);
        var decomposed = new List<DecomposedInspectionRecord>();

        foreach (var sub in submissions)
        {
            if (!string.IsNullOrWhiteSpace(filter?.InspectorName) &&
                !sub.InspectorName.Contains(filter.InspectorName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrWhiteSpace(filter?.MachineCode) &&
                !sub.MachineCode.Contains(filter.MachineCode, StringComparison.OrdinalIgnoreCase))
                continue;

            if (sub.Rows == null || sub.Rows.Count == 0) continue;

            foreach (var row in sub.Rows)
            {
                if (!string.IsNullOrWhiteSpace(filter?.ParameterName) &&
                    !row.ParameterName.Contains(filter.ParameterName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (filter?.IsValid.HasValue == true && row.IsValid != filter.IsValid.Value)
                    continue;

                decomposed.Add(new DecomposedInspectionRecord
                {
                    SubmissionId = sub.Id,
                    FormCode = sub.FormCode,
                    FormRevision = sub.FormRevision,
                    PartCode = sub.PartCode,
                    PartName = sub.PartName,
                    InspectionStationCode = sub.InspectionStationCode,
                    MachineCode = sub.MachineCode,
                    ControlProgramNumber = sub.ControlProgramNumber,
                    SubmittedAt = sub.SubmittedAt,
                    InspectorName = sub.InspectorName,
                    QcSupervisorName = sub.QcSupervisorName,
                    SubmissionStatus = sub.Status,

                    RowNumber = row.RowNumber,
                    ParameterName = row.ParameterName,
                    AcceptanceCriteria = row.ControlItem,
                    ControlMethod = row.ControlMethod,
                    Unit = row.Unit,
                    MinValue = row.MinValue,
                    MaxValue = row.MaxValue,

                    Sample1 = row.Samples != null && row.Samples.Length > 0 ? row.Samples[0] : null,
                    Sample2 = row.Samples != null && row.Samples.Length > 1 ? row.Samples[1] : null,
                    Sample3 = row.Samples != null && row.Samples.Length > 2 ? row.Samples[2] : null,
                    Sample4 = row.Samples != null && row.Samples.Length > 3 ? row.Samples[3] : null,
                    Sample5 = row.Samples != null && row.Samples.Length > 4 ? row.Samples[4] : null,

                    SampleMin = row.SampleMin,
                    SampleMax = row.SampleMax,
                    JobSetupOk = row.JobSetupOk,
                    JobSetupIssues = row.JobSetupIssues,
                    IsValid = row.IsValid
                });
            }
        }

        return decomposed.OrderByDescending(d => d.SubmittedAt).ThenBy(d => d.RowNumber).ToList();
    }

    public async Task<byte[]> GenerateExcelExportAsync(InspectionReportFilter? filter = null)
    {
        var records = await GetDecomposedSubmissionsAsync(filter);

        return await Task.Run(() =>
        {
            var workbook = new Workbook();
            var sheet = workbook.Worksheets[0];
            sheet.Name = "گزارش تفکیکی بازرسی";
            sheet.DisplayRightToLeft = true;

            string[] headers = new[]
            {
                "ردیف", "شناسه فرم", "کد قطعه", "نام قطعه", "ایستگاه", "دستگاه",
                "برنامه کنترل", "تاریخ و زمان", "بازرس", "سرپرست QC", "شماره ردیف",
                "آیتم کنترلی", "معیار پذیرش", "روش کنترل", "واحد", "حد پایین", "حد بالا",
                "نمونه ۱", "نمونه ۲", "نمونه ۳", "نمونه ۴", "نمونه ۵",
                "کمینه نمونه‌ها", "بیشینه نمونه‌ها", "Job Setup", "ایرادات Job Setup", "وضعیت انطباق"
            };

            // Style for header
            var headerStyle = workbook.CreateStyle();
            headerStyle.ForegroundColor = System.Drawing.Color.FromArgb(41, 128, 185);
            headerStyle.Pattern = BackgroundType.Solid;
            headerStyle.Font.Color = System.Drawing.Color.White;
            headerStyle.Font.IsBold = true;
            headerStyle.Font.Size = 10;
            headerStyle.HorizontalAlignment = TextAlignmentType.Center;
            headerStyle.VerticalAlignment = TextAlignmentType.Center;

            for (int col = 0; col < headers.Length; col++)
            {
                sheet.Cells[0, col].PutValue(headers[col]);
                sheet.Cells[0, col].SetStyle(headerStyle);
            }
            sheet.Cells.SetRowHeight(0, 26);

            var styleOk = workbook.CreateStyle();
            styleOk.ForegroundColor = System.Drawing.Color.FromArgb(212, 239, 223);
            styleOk.Pattern = BackgroundType.Solid;
            styleOk.Font.Color = System.Drawing.Color.FromArgb(20, 90, 50);
            styleOk.Font.IsBold = true;
            styleOk.HorizontalAlignment = TextAlignmentType.Center;

            var styleNok = workbook.CreateStyle();
            styleNok.ForegroundColor = System.Drawing.Color.FromArgb(249, 215, 217);
            styleNok.Pattern = BackgroundType.Solid;
            styleNok.Font.Color = System.Drawing.Color.FromArgb(146, 43, 33);
            styleNok.Font.IsBold = true;
            styleNok.HorizontalAlignment = TextAlignmentType.Center;

            var styleCenter = workbook.CreateStyle();
            styleCenter.HorizontalAlignment = TextAlignmentType.Center;

            for (int i = 0; i < records.Count; i++)
            {
                int r = i + 1;
                var item = records[i];

                sheet.Cells[r, 0].PutValue(r);
                sheet.Cells[r, 1].PutValue(item.SubmissionId.ToString().Substring(0, 8));
                sheet.Cells[r, 2].PutValue(item.PartCode);
                sheet.Cells[r, 3].PutValue(item.PartName);
                sheet.Cells[r, 4].PutValue(item.InspectionStationCode);
                sheet.Cells[r, 5].PutValue(item.MachineCode);
                sheet.Cells[r, 6].PutValue(item.ControlProgramNumber);
                sheet.Cells[r, 7].PutValue(item.SubmittedAtShamsi);
                sheet.Cells[r, 8].PutValue(item.InspectorName);
                sheet.Cells[r, 9].PutValue(item.QcSupervisorName);
                sheet.Cells[r, 10].PutValue(item.RowNumber);
                sheet.Cells[r, 11].PutValue(item.ParameterName);
                sheet.Cells[r, 12].PutValue(item.AcceptanceCriteria);
                sheet.Cells[r, 13].PutValue(item.ControlMethod);
                sheet.Cells[r, 14].PutValue(item.Unit);
                if (item.MinValue.HasValue) sheet.Cells[r, 15].PutValue(item.MinValue.Value);
                if (item.MaxValue.HasValue) sheet.Cells[r, 16].PutValue(item.MaxValue.Value);
                if (item.Sample1.HasValue) sheet.Cells[r, 17].PutValue(item.Sample1.Value);
                if (item.Sample2.HasValue) sheet.Cells[r, 18].PutValue(item.Sample2.Value);
                if (item.Sample3.HasValue) sheet.Cells[r, 19].PutValue(item.Sample3.Value);
                if (item.Sample4.HasValue) sheet.Cells[r, 20].PutValue(item.Sample4.Value);
                if (item.Sample5.HasValue) sheet.Cells[r, 21].PutValue(item.Sample5.Value);
                if (item.SampleMin.HasValue) sheet.Cells[r, 22].PutValue(item.SampleMin.Value);
                if (item.SampleMax.HasValue) sheet.Cells[r, 23].PutValue(item.SampleMax.Value);
                sheet.Cells[r, 24].PutValue(item.JobSetupOk.HasValue ? (item.JobSetupOk.Value ? "OK" : "NOK") : "-");
                sheet.Cells[r, 25].PutValue(item.JobSetupIssues);

                sheet.Cells[r, 26].PutValue(item.IsValid ? "OK" : "NOK");
                sheet.Cells[r, 26].SetStyle(item.IsValid ? styleOk : styleNok);

                sheet.Cells[r, 0].SetStyle(styleCenter);
                sheet.Cells[r, 2].SetStyle(styleCenter);
                sheet.Cells[r, 7].SetStyle(styleCenter);
                sheet.Cells[r, 10].SetStyle(styleCenter);
            }

            if (records.Count > 0)
            {
                sheet.AutoFilter.Range = $"A1:AA{records.Count + 1}";
            }

            // Set explicit column widths (avoids SkiaSharp / GDI dependency on Linux)
            int[] colWidths = new int[]
            {
                8, 12, 16, 26, 14, 12, 14, 18, 16, 16,
                10, 22, 28, 14, 8, 10, 10, 10, 10, 10,
                10, 10, 10, 10, 12, 22, 12
            };
            for (int c = 0; c < colWidths.Length; c++)
            {
                sheet.Cells.SetColumnWidth(c, colWidths[c]);
            }

            using var ms = new MemoryStream();
            workbook.Save(ms, SaveFormat.Xlsx);
            return ms.ToArray();
        });
    }

    public async Task<(bool IsSuccess, string ErrorMessage)> SaveSubmissionAsync(InspectionSubmission submission)
    {
        await _writeLock.WaitAsync();
        try
        {
            var workbook = new Workbook(_submissionsPath);
            var sheet = workbook.Worksheets[SheetSubmissions];

            // پیدا کردن آخرین سطر خالی
            int row = 1;
            while (!string.IsNullOrWhiteSpace(sheet.Cells[row, 0].StringValue))
            {
                if (sheet.Cells[row, 0].StringValue == submission.Id.ToString())
                {
                    WriteSubmissionToRow(sheet, row, submission);
                    return SaveWithRetry(workbook, _submissionsPath);
                }
                row++;
            }

            // ردیف جدید
            WriteSubmissionToRow(sheet, row, submission);
            return SaveWithRetry(workbook, _submissionsPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving submission: {ex.Message}");
            return (false, "خطای سیستمی در اکسل: " + ex.Message);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private (bool IsSuccess, string ErrorMessage) SaveWithRetry(Workbook workbook, string path)
    {
        Exception? lastError = null;
        for (int i = 0; i < 3; i++)
        {
            try
            {
                workbook.Save(path);
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                lastError = ex;
                Console.WriteLine($"Retry {i+1} failed: {ex.Message}");
                Thread.Sleep(500);
            }
        }
        return (false, "فایل اکسل توسط پروسه یا شخص دیگری باز است و قفل شده است: " + lastError?.Message);
    }

    private static void WriteSubmissionToRow(Worksheet sheet, int row, InspectionSubmission submission)
    {
        var jsonOptions = new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        sheet.Cells[row, 0].PutValue(submission.Id.ToString());
        sheet.Cells[row, 1].PutValue(JsonSerializer.Serialize(submission, jsonOptions));
        sheet.Cells[row, 2].PutValue(submission.PartCode);
        sheet.Cells[row, 3].PutValue(submission.SubmittedAt.ToString("yyyy-MM-dd HH:mm"));
        sheet.Cells[row, 4].PutValue(submission.InspectorName);
        sheet.Cells[row, 5].PutValue(submission.Status);
    }

    // ─────────────────────────────────────────────
    //   Helper: ایجاد فایل‌های اکسل اولیه
    // ─────────────────────────────────────────────

    private void EnsureFilesExist()
    {
        if (!File.Exists(_masterDataPath))
            CreateMasterDataFile();

        if (!File.Exists(_submissionsPath))
            CreateSubmissionsFile();
    }

    private void CreateMasterDataFile()
    {
        var workbook = new Workbook();

        // شیت Parts
        var parts = workbook.Worksheets[0];
        parts.Name = SheetParts;
        string[] partsHeaders = { "کد قطعه", "نام قطعه", "کد ایستگاه", "نام ایستگاه", "کد ماشین", "شماره برنامه کنترل" };
        for (int i = 0; i < partsHeaders.Length; i++)
            parts.Cells[0, i].PutValue(partsHeaders[i]);

        // داده نمونه (قطعه PMMA-NC)
        parts.Cells[1, 0].PutValue("PMMA-NC-001");
        parts.Cells[1, 1].PutValue("بزل تزئینی ۱ چراغ جلو");
        parts.Cells[1, 2].PutValue("QC-01");
        parts.Cells[1, 3].PutValue("ایستگاه بازرسی ۱");
        parts.Cells[1, 4].PutValue("INJ-214");
        parts.Cells[1, 5].PutValue("I-PC-0039");

        // شیت Parameters
        var parameters = workbook.Worksheets.Add(SheetParameters);
        string[] paramHeaders = { "کد قطعه", "شماره ردیف", "نام پارامتر", "نوع", "معیار پذیرش", "روش کنترل", "حد پایین", "حد بالا", "واحد" };
        for (int i = 0; i < paramHeaders.Length; i++)
            parameters.Cells[0, i].PutValue(paramHeaders[i]);

        // پارامترهای نمونه برای PMMA-NC-001
        var sampleParams = new[]
        {
            new object?[] { "PMMA-NC-001", 1, "نوع مواد", "مواد", "PMMA- NC مطابقت لیبل روی بسته بندی مواد", "چشمی", null, null, "" },
            new object?[] { "PMMA-NC-001", 2, "پارامتر A (ظاهری)", "ظاهری", "بدون پلیسه و پرشدگی باشد و مطابق نمونه شاهد باشد", "چشمی", null, null, "" },
            new object?[] { "PMMA-NC-001", 3, "پارامتر B (ظاهری)", "ظاهری", "بدون پلیسه و پرنشدگی و ترک و شکستگی باشد و مطابق نمونه شاهد باشد", "چشمی", null, null, "" },
            new object?[] { "PMMA-NC-001", 4, "وزن قطعه", "وزنی", "17.7 ± 0.265 gr", "ترازو", 17.435, 17.965, "gr" },
            new object?[] { "PMMA-NC-001", 5, "ظاهری", "ظاهری", "بدون پلیسه، پرنشدگی، فرورفتگی، دفرمگی و شکستگی باشد", "چشمی", null, null, "" },
            new object?[] { "PMMA-NC-001", 6, "مونتاژی", "مونتاژی", "پس از مونتاژ قطعات مربوطه بر روی پنل، مجموعه از نظر مونتاژی با نمونه شاهد مطابقت داشته باشد", "چشمی", null, null, "" },
        };

        for (int r = 0; r < sampleParams.Length; r++)
        {
            for (int c = 0; c < sampleParams[r].Length; c++)
            {
                if (sampleParams[r][c] != null)
                    parameters.Cells[r + 1, c].PutValue(sampleParams[r][c]);
            }
        }

        workbook.Save(_masterDataPath);
    }

    private void CreateSubmissionsFile()
    {
        var workbook = new Workbook();
        var sheet = workbook.Worksheets[0];
        sheet.Name = SheetSubmissions;
        string[] headers = { "ID", "JSON Data", "کد قطعه", "تاریخ ثبت", "نام بازرس", "وضعیت" };
        for (int i = 0; i < headers.Length; i++)
            sheet.Cells[0, i].PutValue(headers[i]);
        workbook.Save(_submissionsPath);
    }
}
