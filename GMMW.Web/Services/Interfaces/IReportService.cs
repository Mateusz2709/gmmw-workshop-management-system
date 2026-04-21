using System;
using GMMW.Web.Models.ViewModels;

namespace GMMW.Web.Services.Interfaces
{
    // Defines the contract for the monthly and daily reporting queries used by the reports module.
    public interface IReportService
    {
        // Returns the detailed monthly parts report for the selected month and year.
        Task<List<MonthlyPartsReportItemViewModel>> GetMonthlyPartsReportAsync(int month, int year);

        // Returns the grouped monthly summary showing how many parts of each type were used.
        Task<List<MonthlyPartTypeSummaryItemViewModel>> GetMonthlyPartTypeSummaryAsync(int month, int year);

        // Returns the monthly repair summary including repair count and average repair cost.
        Task<MonthlyRepairSummaryViewModel> GetMonthlyRepairSummaryAsync(int month, int year);

        // Returns the classes scheduled for the selected day.
        Task<List<DailyClassReportItemViewModel>> GetDailyClassesReportAsync(DateTime selectedDate);

        // Returns the classes delivered by one selected volunteer in the chosen month and year.
        Task<List<VolunteerClassReportItemViewModel>> GetVolunteerClassReportAsync(int month, int year, string volunteerUserId);
    }
}