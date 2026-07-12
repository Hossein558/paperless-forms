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
            int row = 1;

            while (!string.IsNullOrWhiteSpace(sheet.Cells[row, 0].StringValue))
            {
                if (sheet.Cells[row, 0].StringValue.Trim().Equals(partCode.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    var part = MapRowToPart(sheet, row);
                    part.Parameters = GetParametersForPart(partCode);
                    return part;
                }
                row++;
            }
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

    private List<InspectionParameter> GetParametersForPart(string partCode)
    {
        var parameters = new List<InspectionParameter>();
        var workbook = new Workbook(_masterDataPath);
        var sheet = workbook.Worksheets[SheetParameters];
        int row = 1;

        while (!string.IsNullOrWhiteSpace(sheet.Cells[row, 0].StringValue))
        {
            if (sheet.Cells[row, 0].StringValue.Trim().Equals(partCode.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                parameters.Add(new InspectionParameter
                {
                    RowNumber = (int)(sheet.Cells[row, 1].DoubleValue),
                    ParameterName = sheet.Cells[row, 2].StringValue.Trim(),
                    ParameterType = sheet.Cells[row, 3].StringValue.Trim(),
                    AcceptanceCriteria = sheet.Cells[row, 4].StringValue.Trim(),
                    ControlMethod = sheet.Cells[row, 5].StringValue.Trim(),
                    MinValue = string.IsNullOrWhiteSpace(sheet.Cells[row, 6].StringValue) ? null : sheet.Cells[row, 6].DoubleValue,
                    MaxValue = string.IsNullOrWhiteSpace(sheet.Cells[row, 7].StringValue) ? null : sheet.Cells[row, 7].DoubleValue,
                    Unit = sheet.Cells[row, 8].StringValue.Trim(),
                });
            }
            row++;
        }
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
