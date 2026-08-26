using System;

namespace PaperlessForms.Core.Models;

/// <summary>
/// یک رکورد تفکیک‌شده بازرسی برای گزارش‌گیری و خروجی اکسل
/// هر ردیف مشخصه فنی همراه با اطلاعات فرم والد به صورت جداگانه در یک سطر قرار می‌گیرد
/// </summary>
public class DecomposedInspectionRecord
{
    // ─── اطلاعات هدر فرم ──────────────────────────────────
    public Guid SubmissionId { get; set; }
    public string FormCode { get; set; } = string.Empty;
    public string FormRevision { get; set; } = string.Empty;
    public string PartCode { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string InspectionStationCode { get; set; } = string.Empty;
    public string MachineCode { get; set; } = string.Empty;
    public string ControlProgramNumber { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string InspectorName { get; set; } = string.Empty;
    public string QcSupervisorName { get; set; } = string.Empty;
    public string SubmissionStatus { get; set; } = string.Empty;

    // ─── مشخصه کنترلی ─────────────────────────────────────
    public int RowNumber { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public string AcceptanceCriteria { get; set; } = string.Empty;
    public string ControlMethod { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }

    // ─── مقادیر ۵ نمونه ──────────────────────────────────
    public double? Sample1 { get; set; }
    public double? Sample2 { get; set; }
    public double? Sample3 { get; set; }
    public double? Sample4 { get; set; }
    public double? Sample5 { get; set; }

    // ─── محاسبات و نتایج ─────────────────────────────────
    public double? SampleMin { get; set; }
    public double? SampleMax { get; set; }
    public bool? JobSetupOk { get; set; }
    public string JobSetupIssues { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string ResultStatus => IsValid ? "OK" : "NOK";
    public string ResultTitle => IsValid ? "منطبق" : "نامنطبق";

    public string SubmittedAtShamsi
    {
        get
        {
            var pc = new System.Globalization.PersianCalendar();
            return $"{pc.GetYear(SubmittedAt):D4}/{pc.GetMonth(SubmittedAt):D2}/{pc.GetDayOfMonth(SubmittedAt):D2} {SubmittedAt:HH:mm}";
        }
    }

    public string SubmittedDateShamsi
    {
        get
        {
            var pc = new System.Globalization.PersianCalendar();
            return $"{pc.GetYear(SubmittedAt):D4}/{pc.GetMonth(SubmittedAt):D2}/{pc.GetDayOfMonth(SubmittedAt):D2}";
        }
    }
}

public static class PersianDateHelper
{
    public static DateTime? ToGregorian(string? shamsiDate)
    {
        if (string.IsNullOrWhiteSpace(shamsiDate)) return null;
        try
        {
            var parts = shamsiDate.Replace("-", "/").Trim().Split('/');
            if (parts.Length == 3 &&
                int.TryParse(parts[0], out int y) &&
                int.TryParse(parts[1], out int m) &&
                int.TryParse(parts[2], out int d))
            {
                var pc = new System.Globalization.PersianCalendar();
                return pc.ToDateTime(y, m, d, 0, 0, 0, 0);
            }
        }
        catch { }
        return null;
    }

    public static string ToShamsi(DateTime dt)
    {
        var pc = new System.Globalization.PersianCalendar();
        return $"{pc.GetYear(dt):D4}/{pc.GetMonth(dt):D2}/{pc.GetDayOfMonth(dt):D2}";
    }
}

/// <summary>
/// مدل فیلترهای گزارش‌گیری بازرسی
/// </summary>
public class InspectionReportFilter
{
    public string? PartCode { get; set; }
    public string? InspectorName { get; set; }
    public string? ParameterName { get; set; }
    public string? MachineCode { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? IsValid { get; set; } // null = همه، true = فقط OK، false = فقط NOK
}
