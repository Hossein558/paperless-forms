using PaperlessForms.Core.Models;

namespace PaperlessForms.Core.Interfaces;

public interface IPartRepository
{
    Task<List<Part>> GetAllPartsAsync();
    Task<Part?> GetPartByCodeAsync(string partCode);
}

public interface IInspectionRepository
{
    Task<List<InspectionSubmission>> GetSubmissionsAsync(string? partCode = null, DateTime? from = null, DateTime? to = null);
    Task<InspectionSubmission?> GetByIdAsync(Guid id);
    Task<(bool IsSuccess, string ErrorMessage)> SaveSubmissionAsync(InspectionSubmission submission);
    Task<List<DecomposedInspectionRecord>> GetDecomposedSubmissionsAsync(InspectionReportFilter? filter = null);
    Task<byte[]> GenerateExcelExportAsync(InspectionReportFilter? filter = null);
}
