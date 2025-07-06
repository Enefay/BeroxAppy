using BeroxAppy.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace BeroxAppy.Customers
{
    public class CustomerDto : FullAuditedEntityDto<Guid>
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

        [Range(0, 100)]
        public decimal DiscountRate { get; set; } // %

        public decimal TotalDebt { get; set; } // Toplam borç

        public bool IsActive { get; set; } = true;

        // Display için hesaplanan alanlar
        public string GenderDisplay { get; set; } // "Erkek", "Kadın", "Belirtilmemiş"
        public int? Age { get; set; } // Yaş hesaplama
        public string DebtStatusDisplay { get; set; } // "Borcu Yok", "₺150 Borç"
    }
}
