using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BeroxAppy.Reservations;
using BeroxAppy.Finance;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using BeroxAppy.Enums;
using BeroxAppy.Finances;

namespace BeroxAppy.Web.Pages.Reservations
{
    public class CompleteReservationModalModel : AbpPageModel
    {
        private readonly IReservationAppService _reservationAppService;
        private readonly IPaymentAppService _paymentAppService;

        public ReservationDto Reservation { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal CustomerDiscountRate { get; set; }
        public List<PaymentDto> ExistingPayments { get; set; }
        public Guid ReservationId { get; set; }

        public CompleteReservationModalModel(
            IReservationAppService reservationAppService,
            IPaymentAppService paymentAppService)
        {
            _reservationAppService = reservationAppService;
            _paymentAppService = paymentAppService;
        }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            ReservationId = id;
            Reservation = await _reservationAppService.GetAsync(id);

            // Ödeme bilgilerini al
            PaidAmount = await _paymentAppService.GetReservationPaidAmountAsync(id);
            RemainingAmount = await _paymentAppService.GetReservationRemainingAmountAsync(id);

            // Müþteri indirim oranýný al (Customer service'den alýnabilir)
            CustomerDiscountRate = 0; // TODO: Customer service'den al

            // Mevcut ödemeleri al
            var paymentsResult = await _paymentAppService.GetReservationPaymentsAsync(id);
            ExistingPayments = paymentsResult.Items.ToList();

            return Page();
        }
    }
}
