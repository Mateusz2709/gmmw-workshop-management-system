using GMMW.Web.Models.Domain;
using GMMW.Web.Models.ViewModels;

namespace GMMW.Web.Services.Interfaces
{
    public interface IAttendanceService
    {
        Task<bool> AttendanceExistsAsync(int workshopClassId, int motoristId);
        Task RecordAttendanceAsync(AttendanceRecordViewModel model);
        Task<List<ClassAttendance>> GetAttendanceByClassIdAsync(int workshopClassId);

        Task RemoveAttendanceAsync(int workshopClassId, int motoristId);

        Task<List<ClassAttendanceCountItemViewModel>> GetAttendanceCountByClassReportAsync(int year, int month, string? deliveredByUserId);

        Task<int> GetAttendanceReportCountAsync(int year, int month, string? deliveredByUserId);
        Task<List<AttendanceReportItemViewModel>> SearchAttendanceReportAsync(int year, int month, string? deliveredByUserId, int pageNumber, int pageSize);
    }
}
