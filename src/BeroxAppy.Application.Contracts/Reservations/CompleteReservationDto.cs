using BeroxAppy.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeroxAppy.Reservations
{
    public class CompleteReservationDto
    {
        public Guid ReservationId { get; set; }
        public List<ServicePriceChangeDto> ServiceChanges { get; set; } = new();
        public decimal AdditionalDiscount { get; set; }
        public string DiscountReason { get; set; }
        public decimal AdditionalCharge { get; set; }
        public string ChargeReason { get; set; }
        public decimal PaymentAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string PaymentNote { get; set; }
    }
    public class ServicePriceChangeDto
    {
        public Guid ReservationDetailId { get; set; }
        public decimal NewPrice { get; set; }
    }
}
