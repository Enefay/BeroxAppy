using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;
using BeroxAppy.Services;
using BeroxAppy.Finances.FinanceAppDtos;
using BeroxAppy.Finances;
using BeroxAppy.Reservations;
using System.Collections.Generic;
using System.Globalization;

namespace BeroxAppy.Web.Pages.Finance.Dashboard
{
    public class IndexModel : BeroxAppyPageModel
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

        [BindProperty]
        public DateTime SelectedDate { get; set; } = DateTime.Now.Date;

        public DashboardDto Dashboard { get; set; }
        public CashRegisterDto CashRegister { get; set; }
        public List<ReservationDto> TodayReservations { get; set; }

        public async Task OnGetAsync(string? date)
        {
            if (!string.IsNullOrEmpty(date))
            {
                // Kabul edilecek formatlar
                var formats = new[] { "yyyy-MM-dd", "dd.MM.yyyy" };
                if (DateTime.TryParseExact(
                        date,
                        formats,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var parsedDate))
                {
                    SelectedDate = parsedDate;
                }
                else
                {
                    // Güvenlik amaçlý fallback
                    SelectedDate = DateTime.Today;
                    ModelState.AddModelError("date", "Tarih formatý geçerli deðil.");
                }
            }
            else
            {
                SelectedDate = DateTime.Today;
            }
            // Dashboard verileri
            Dashboard = await _financeAppService.GetDashboardAsync(SelectedDate);

            // Kasa durumu
            CashRegister = await _paymentAppService.GetTodaysCashRegisterAsync();

            // Bugünkü rezervasyonlar
            TodayReservations = await _reservationAppService.GetTodayReservationsAsync();
        }

        public async Task<IActionResult> OnPostCloseCashAsync()
        {
            try
            {
                var cashRegister = await _paymentAppService.GetTodaysCashRegisterAsync();
                if (cashRegister.IsClosed)
                {
                    return new JsonResult(new { success = false, message = "Kasa zaten kapatýlmýþ!" });
                }

                // Basit kapanýþ - teorik bakiye ile
                var theoreticalBalance = cashRegister.OpeningBalance + cashRegister.TotalCashIn - cashRegister.TotalCashOut;
                await _paymentAppService.CloseCashRegisterAsync(cashRegister.Id, theoreticalBalance, "Otomatik kapanýþ");

                return new JsonResult(new { success = true, message = "Kasa baþarýyla kapatýldý." });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
    }
}