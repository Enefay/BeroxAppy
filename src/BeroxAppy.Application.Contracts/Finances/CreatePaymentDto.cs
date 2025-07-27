using BeroxAppy.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeroxAppy.Finances
{
    public class CreatePaymentDto
    {
        public Guid CustomerId { get; set; }
        public Guid? ReservationId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Description { get; set; }
        public bool IsRefund { get; set; }
        public Guid? CashRegisterId { get; set; }
    }
}
