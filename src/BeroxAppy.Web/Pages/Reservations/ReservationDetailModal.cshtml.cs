using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BeroxAppy.Reservations;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using BeroxAppy.Enums;

namespace BeroxAppy.Web.Pages.Reservations
{
    public class ReservationDetailModalModel : AbpPageModel
    {
        private readonly IReservationAppService _reservationAppService;

        public ReservationDto Reservation { get; set; }

        public ReservationDetailModalModel(IReservationAppService reservationAppService)
        {
            _reservationAppService = reservationAppService;
        }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            Reservation = await _reservationAppService.GetAsync(id);
            return Page();
        }

        public string GetStatusBadgeColor()
        {
            return Reservation.Status switch
            {
                ReservationStatus.Pending => "warning",
                ReservationStatus.NoShow => "danger",
                ReservationStatus.Arrived => "success",
                _ => "secondary"
            };
        }
        public string GetPaymentBadgeColor()
        {
            return Reservation.PaymentStatus switch
            {
                PaymentStatus.Pending => "warning",
                PaymentStatus.Partial => "info",
                PaymentStatus.Paid => "success",
                PaymentStatus.Refunded => "danger",
                _ => "secondary"
            };
        }
    }
}
