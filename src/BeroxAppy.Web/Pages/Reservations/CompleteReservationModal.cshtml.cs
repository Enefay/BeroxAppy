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
using BeroxAppy.Customers;

namespace BeroxAppy.Web.Pages.Reservations
{
    public class CompleteReservationModalModel : AbpPageModel
    {
        private readonly IReservationAppService _reservationAppService;
        private readonly IPaymentAppService _paymentAppService;
        private readonly ICustomerAppService _customerAppService; 

        public ReservationDto Reservation { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal CustomerDiscountRate { get; set; }
        public List<PaymentDto> ExistingPayments { get; set; }
        public Guid ReservationId { get; set; }

        public CompleteReservationModalModel(
            IReservationAppService reservationAppService,
            IPaymentAppService paymentAppService,
            ICustomerAppService customerAppService)
        {
            _reservationAppService = reservationAppService;
            _paymentAppService = paymentAppService;
            _customerAppService = customerAppService;
        }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            ReservationId = id;
            Reservation = await _reservationAppService.GetAsync(id);

            // Müþteri indirim oranýný al - DÜZELTÝLDÝ
            var customer = await _customerAppService.GetAsync(Reservation.CustomerId);
            CustomerDiscountRate = customer.DiscountRate;


            // Ödeme bilgilerini al
            PaidAmount = await _paymentAppService.GetReservationPaidAmountAsync(id);
            RemainingAmount = await _paymentAppService.GetReservationRemainingAmountAsync(id);

            // Mevcut ödemeleri al
            var paymentsResult = await _paymentAppService.GetReservationPaymentsAsync(id);
            ExistingPayments = paymentsResult.Items.ToList();

            return Page();
        }
    }
}
