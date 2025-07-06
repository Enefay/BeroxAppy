using BeroxAppy.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace BeroxAppy.Employees
{
    public class EmployeeDto : FullAuditedEntityDto<Guid>
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; }

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; }

        [MaxLength(100)]
        public string Email { get; set; }

        public Guid? UserId { get; set; } // ABP User ile ilişki

        public EmployeeType EmployeeType { get; set; }

        public Gender ServiceGender { get; set; }

        [MaxLength(7)]
        public string CalendarColor { get; set; } = "#3498db";

        public bool CanTakeOnlineReservation { get; set; } = true;

        [Range(0, double.MaxValue)]
        public decimal FixedSalary { get; set; }

        [Range(0, 100)]
        public decimal ServiceCommissionRate { get; set; } // %

        [Range(0, 100)]
        public decimal ProductCommissionRate { get; set; } // %

        [Range(0, 100)]
        public decimal PackageCommissionRate { get; set; } // %

        public bool IsActive { get; set; } = true;

        // Create için gerekli alanlar
        public string Password { get; set; } // Kullanıcı şifresi
        public string UserName { get; set; } // Kullanıcı adı

        // Display için hesaplanan alanlar
        public string FullName { get; set; } // FirstName + LastName
        public string EmployeeTypeDisplay { get; set; } // "Personel", "Sekreter" vb.
        public string ServiceGenderDisplay { get; set; } // "Erkek", "Kadın", "Unisex"
        public bool HasUser { get; set; } // Kullanıcısı var mı?
        public string UserStatus { get; set; } // "Aktif", "Pasif", "Kullanıcı Yok"
    }
}
