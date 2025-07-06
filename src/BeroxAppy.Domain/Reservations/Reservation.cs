using BeroxAppy.Customers;
using BeroxAppy.Enums;
using BeroxAppy.Finance;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BeroxAppy.Reservations
{
    // Randevular
    public class Reservation : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }

        public Guid CustomerId { get; set; }
        public Customer Customer { get; set; }

        [MaxLength(500)]
        public string Note { get; set; }

        public DateTime ReservationDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public decimal TotalServicePrice { get; set; } // Tüm hizmetlerin toplam fiyatı
        public decimal FinalPrice { get; set; } // İndirim/ekstra sonrası fiyat

        public decimal? DiscountAmount { get; set; }
        public decimal? ExtraAmount { get; set; }

        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        public bool IsWalkIn { get; set; } // true: adisyon, false: rezervasyon

        public bool ReminderSent { get; set; } = false;

        // Navigation properties
        public ICollection<ReservationDetail> ReservationDetails { get; set; }
        public ICollection<Payment> Payments { get; set; }
    }

}
