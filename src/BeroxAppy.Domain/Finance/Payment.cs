using BeroxAppy.Customers;
using BeroxAppy.Enums;
using BeroxAppy.Reservations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BeroxAppy.Finance
{
    // Tahsilat/Ödeme
    public class Payment : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }

        public Guid CustomerId { get; set; }
        public Customer Customer { get; set; }

        public Guid? ReservationId { get; set; }
        public Reservation Reservation { get; set; }

        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public DateTime PaymentDate { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public bool IsRefund { get; set; } = false;

        // Kasa için
        public Guid? CashRegisterId { get; set; }
        public CashRegister CashRegister { get; set; }
    }
}
