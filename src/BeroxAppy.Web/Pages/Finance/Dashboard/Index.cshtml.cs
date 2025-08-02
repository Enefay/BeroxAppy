using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;
using BeroxAppy.Services;
using BeroxAppy.Finances.FinanceAppDtos;
using BeroxAppy.Finances;
using BeroxAppy.Reservations;
using System.Collections.Generic;

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

        public async Task OnGetAsync(DateTime? date = null)
        {
            SelectedDate = date?.Date ?? DateTime.Now.Date;

            // Dashboard verileri
            Dashboard = await _financeAppService.GetDashboardAsync(SelectedDate);

            // Kasa durumu
            CashRegister = await _paymentAppService.GetTodaysCashRegisterAsync();

            // Bug�nk� rezervasyonlar
            TodayReservations = await _reservationAppService.GetTodayReservationsAsync();
        }

        public async Task<IActionResult> OnPostCloseCashAsync()
        {
            try
            {
                var cashRegister = await _paymentAppService.GetTodaysCashRegisterAsync();
                if (cashRegister.IsClosed)
                {
                    return new JsonResult(new { success = false, message = "Kasa zaten kapat�lm��!" });
                }

                // Basit kapan�� - teorik bakiye ile
                var theoreticalBalance = cashRegister.OpeningBalance + cashRegister.TotalCashIn - cashRegister.TotalCashOut;
                await _paymentAppService.CloseCashRegisterAsync(cashRegister.Id, theoreticalBalance, "Otomatik kapan��");

                return new JsonResult(new { success = true, message = "Kasa ba�ar�yla kapat�ld�." });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
    }
}