using BeroxAppy.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeroxAppy.Finances
{
    public class PaymentDto
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public Guid? ReservationId { get; set; }
        public DateTime? ReservationDate { get; set; }
        public string ReservationDisplay { get; set; }
        public decimal Amount { get; set; }
        public string AmountDisplay { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string PaymentMethodDisplay { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Description { get; set; }
        public bool IsRefund { get; set; }
        public string PaymentTypeDisplay { get; set; }
        public Guid? CashRegisterId { get; set; }
    }
}
