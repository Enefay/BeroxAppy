using BeroxAppy.Customers;
using BeroxAppy.Reservations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BeroxAppy.Notifications
{
    // SMS/Bildirim Geçmişi
    public class NotificationHistory : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }

        public Guid CustomerId { get; set; }
        public Customer Customer { get; set; }

        public Guid? ReservationId { get; set; }
        public Reservation Reservation { get; set; }

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; }

        [Required]
        [MaxLength(500)]
        public string Message { get; set; }

        public bool IsSent { get; set; }

        public DateTime? SentDate { get; set; }

        [MaxLength(200)]
        public string ErrorMessage { get; set; }
    }
}
