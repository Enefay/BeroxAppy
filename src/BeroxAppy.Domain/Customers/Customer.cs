using BeroxAppy.Enums;
using BeroxAppy.Finance;
using BeroxAppy.Reservations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;

namespace BeroxAppy.Customers
{
    // Müşteriler
    public class Customer : FullAuditedAggregateRoot<Guid>
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; }

        public DateTime? BirthDate { get; set; }

        [MaxLength(100)]
        public string Email { get; set; }

        public Gender Gender { get; set; }

        [MaxLength(1000)]
        public string Note { get; set; }

        [MaxLength(50)]
        public string Instagram { get; set; }

        public decimal DiscountRate { get; set; } // %

        public decimal TotalDebt { get; set; } // Toplam borç

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<Reservation> Reservations { get; set; }
        public ICollection<Payment> Payments { get; set; }
    }
}
