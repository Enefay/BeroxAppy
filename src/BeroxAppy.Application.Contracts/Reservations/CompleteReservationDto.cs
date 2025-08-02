using BeroxAppy.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeroxAppy.Reservations
{
    public class CompleteReservationDto
    {

        [Required]
        public Guid ReservationId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "İndirim tutarı negatif olamaz")]
        public decimal AdditionalDiscount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Ek ücret negatif olamaz")]
        public decimal AdditionalCharge { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Ödeme tutarı negatif olamaz")]
        public decimal PaymentAmount { get; set; }

        public PaymentMethod? PaymentMethod { get; set; }
        public string? PaymentNote { get; set; }

        public List<ServicePriceChangeDto>? ServiceChanges { get; set; } = new();


    }
    public class ServicePriceChangeDto
    {
        public Guid ReservationDetailId { get; set; }
        public decimal NewPrice { get; set; }
    }
}
