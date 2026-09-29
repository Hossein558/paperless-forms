namespace PaperlessForms.Core.Models;

/// <summary>
/// یک رکورد بازرسی کامل که کاربر ثبت می‌کند
/// </summary>
public class InspectionSubmission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FormCode { get; set; } = "F-IT-23/05";
    public string FormRevision { get; set; } = "01";

    // اطلاعات هدر
    public string PartCode { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string InspectionStationCode { get; set; } = string.Empty;
    public string MachineCode { get; set; } = string.Empty;
    public string ControlProgramNumber { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 1;

    // تاریخ و اطلاعات بازرس
    public DateTime SubmittedAt { get; set; } = DateTime.Now;
    public string InspectorName { get; set; } = string.Empty;
    public string QcSupervisorName { get; set; } = string.Empty;

    // ردیف‌های بازرسی
    public List<InspectionRow> Rows { get; set; } = new();

    public string Status { get; set; } = "Draft"; // Draft, Submitted, Approved
}

/// <summary>
/// هر ردیف از جدول بازرسی (شامل ۵ نمونه)
/// </summary>
public class InspectionRow
{
    public int RowNumber { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public string ControlItem { get; set; } = string.Empty;
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string ControlMethod { get; set; } = string.Empty;

    // ۵ نمونه اندازه‌گیری
    public double?[] Samples { get; set; } = new double?[5];

    // Job Setup
    public bool? JobSetupOk { get; set; }        // true=OK, false=NOK
    public string JobSetupIssues { get; set; } = string.Empty;

    // محاسبه خودکار
    public double? SampleMin => Samples.Where(s => s.HasValue).Select(s => s!.Value).DefaultIfEmpty().Min() is 0 && !Samples.Any(s => s.HasValue) ? null : Samples.Where(s => s.HasValue).Select(s => s!.Value).DefaultIfEmpty().Min();
    public double? SampleMax => Samples.Where(s => s.HasValue).Any() ? Samples.Where(s => s.HasValue).Select(s => s!.Value).Max() : null;

    // وضعیت اعتبارسنجی
    public bool IsValid
    {
        get
        {
            var filledSamples = Samples.Where(s => s.HasValue).ToList();
            if (filledSamples.Count == 0) return true;

            // ۱. آیتم‌های مقداری و دارای تلرانس (کمی)
            if (MinValue.HasValue || MaxValue.HasValue)
            {
                return filledSamples.All(s =>
                    (!MinValue.HasValue || s!.Value >= MinValue.Value) &&
                    (!MaxValue.HasValue || s!.Value <= MaxValue.Value));
            }

            // ۲. آیتم‌های کیفی و وصفی (1=OK, 0=NOK):
            // اگر حتی یکی از نمونه‌ها 0 (Not OK) باشد، کل ردیف نامنطبق (false) می‌شود
            return filledSamples.All(s => s!.Value != 0);
        }
    }
}
