namespace PaperlessForms.Core.Models;

/// <summary>
/// اطلاعات پایه قطعه/محصول
/// </summary>
public class Part
{
    public string PartCode { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string InspectionStationCode { get; set; } = string.Empty;
    public string InspectionStationName { get; set; } = string.Empty;
    public string MachineCode { get; set; } = string.Empty;
    public string ControlProgramNumber { get; set; } = string.Empty;
    public List<InspectionParameter> Parameters { get; set; } = new();
}

/// <summary>
/// پارامتر کنترلی هر قطعه (سوالات بازرسی)
/// </summary>
public class InspectionParameter
{
    public int RowNumber { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public string ParameterType { get; set; } = string.Empty; // ظاهری، وزنی، ابعادی ...
    public string AcceptanceCriteria { get; set; } = string.Empty;
    public string ControlMethod { get; set; } = string.Empty; // چشمی، کولیس، ترازو ...
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public string Unit { get; set; } = string.Empty;
}
