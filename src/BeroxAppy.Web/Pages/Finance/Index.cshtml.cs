using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BeroxAppy.Finances;
using BeroxAppy.Finances.FinanceAppDtos;
using BeroxAppy.Reservations;
using BeroxAppy.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace BeroxAppy.Web.Pages.Finance
{
    public class IndexModel : AbpPageModel
    {
        private readonly IFinanceAppService _financeAppService;
        private readonly IPaymentAppService _paymentAppService;
        private readonly IReservationAppService _reservationAppService;

        public IndexModel(
            IFinanceAppService financeAppService,
            IPaymentAppService paymentAppService,
            IReservationAppService reservationAppService)
        {
            _financeAppService = financeAppService;
            _paymentAppService = paymentAppService;
            _reservationAppService = reservationAppService;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime SelectedDate { get; set; } = DateTime.Now.Date;

        public DashboardDto DashboardData { get; set; }
        public CashRegisterDto CashRegister { get; set; }
        public DailyFinancialSummaryDto DailySummary { get; set; }
        public List<ReservationDto> TodayReservations { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                // Ana dashboard verilerini al
                DashboardData = await _financeAppService.GetDashboardAsync(SelectedDate);

                // Günlük kasa durumunu al
                if (SelectedDate.Date == DateTime.Now.Date)
                {
                    // Bugün ise güncel kasa bilgisini al
                    CashRegister = await _paymentAppService.GetTodaysCashRegisterAsync();

                    if (CashRegister != null)
                    {
                        // Teorik bakiye hesapla
                        CashRegister.TheoreticalBalance = CashRegister.OpeningBalance +
                                                         CashRegister.TotalCashIn -
                                                         CashRegister.TotalCashOut;
                    }
                }

                // Günlük finansal özeti al
                DailySummary = await _financeAppService.GetOrCreateDailySummaryAsync(SelectedDate);

                // Seçilen günün rezervasyonlarýný al
                if (SelectedDate.Date == DateTime.Now.Date)
                {
                    TodayReservations = await _reservationAppService.GetTodayReservationsAsync();
                }
                else
                {
                    // Geçmiþ tarih için rezervasyonlarý filtreli al
                    var reservationFilter = new GetReservationsInput
                    {
                        StartDate = SelectedDate.Date,
                        EndDate = SelectedDate.Date.AddDays(1),
                        MaxResultCount = 100,
                        SkipCount = 0
                    };

                    var reservationsResult = await _reservationAppService.GetListAsync(reservationFilter);
                    TodayReservations = reservationsResult.Items.ToList();
                }
            }
            catch (Exception ex)
            {
                // Hata loglama
                Logger.LogError(ex, "Dashboard yüklenirken hata oluþtu: {Date}", SelectedDate);

                // Varsayýlan deðerler
                DashboardData = new DashboardDto
                {
                    Date = SelectedDate,
                    TodayIncome = 0,
                    TodayExpenses = 0,
                    TodayProfit = 0,
                    PendingCommissions = 0,
                    EmployeeCommissions = new List<EmployeeCommissionSummaryDto>()
                };
            }
        }

        public async Task<IActionResult> OnPostCloseCashRegisterAsync(Guid cashRegisterId, decimal actualClosingBalance, string note)
        {
            try
            {
                var result = await _paymentAppService.CloseCashRegisterAsync(cashRegisterId, actualClosingBalance, note);

                return new JsonResult(new { success = true, message = "Kasa baþarýyla kapatýldý!" });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Kasa kapatýlýrken hata oluþtu: {CashRegisterId}", cashRegisterId);
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostCreateCashRegisterAsync()
        {
            try
            {
                //var cashRegister = await _paymentAppService.GetOrCreateTodaysCashRegisterAsync();

                //return new JsonResult(new { success = true, message = "Kasa baþarýyla açýldý!", cashRegisterId = cashRegister.Id });
                return new JsonResult(new { success = true, message = "Kasa baþarýyla açýldý!"});

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Kasa açýlýrken hata oluþtu");
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        // Hýzlý istatistikler için helper methodlar
        public string GetReservationCompletionRate()
        {
            if (!TodayReservations.Any()) return "0%";

            var completedCount = TodayReservations.Count(r => r.Status == Enums.ReservationStatus.Arrived);
            var rate = (completedCount * 100.0) / TodayReservations.Count;

            return $"{rate:F1}%";
        }

        public string GetPaymentCompletionRate()
        {
            if (!TodayReservations.Any()) return "0%";

            var paidCount = TodayReservations.Count(r => r.PaymentStatus == Enums.PaymentStatus.Paid);
            var rate = (paidCount * 100.0) / TodayReservations.Count;

            return $"{rate:F1}%";
        }

        public decimal GetTotalUnpaidAmount()
        {
            return TodayReservations
                .Where(r => r.PaymentStatus != Enums.PaymentStatus.Paid)
                .Sum(r => r.FinalPrice);
        }

        public string GetDayStatus()
        {
            if (SelectedDate.Date > DateTime.Now.Date)
                return "Gelecek";
            else if (SelectedDate.Date < DateTime.Now.Date)
                return "Geçmiþ";
            else
                return "Bugün";
        }

        public bool IsTodaySelected => SelectedDate.Date == DateTime.Now.Date;

        public string GetFormattedDate()
        {
            var today = DateTime.Now.Date;

            if (SelectedDate.Date == today)
                return "Bugün";
            else if (SelectedDate.Date == today.AddDays(-1))
                return "Dün";
            else if (SelectedDate.Date == today.AddDays(1))
                return "Yarýn";
            else
                return SelectedDate.ToString("dd MMMM yyyy dddd");
        }
    }
}

// Extension sýnýfý - Dashboard için ek metodlar
public static class DashboardExtensions
{
    public static string ToDisplayString(this decimal amount)
    {
        return $"?{amount:N2}";
    }

    public static string ToPercentageString(this decimal value)
    {
        return $"%{value:F1}";
    }

    public static string GetTrendIcon(this decimal currentValue, decimal previousValue)
    {
        if (currentValue > previousValue)
            return "fas fa-arrow-up text-success";
        else if (currentValue < previousValue)
            return "fas fa-arrow-down text-danger";
        else
            return "fas fa-minus text-muted";
    }

    public static string GetProgressBarClass(this decimal percentage)
    {
        return percentage switch
        {
            >= 80 => "bg-success",
            >= 60 => "bg-info",
            >= 40 => "bg-warning",
            _ => "bg-danger"
        };
    }
}