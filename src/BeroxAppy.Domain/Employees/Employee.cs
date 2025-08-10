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
using Volo.Abp.MultiTenancy;

namespace BeroxAppy.Employees
{
    public class Employee : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }

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

        public Gender ServiceGender { get; set; } // Hangi cinsiyete hizmet veriyor

        [MaxLength(7)]
        public string CalendarColor { get; set; } // Hex color code

        public bool CanTakeOnlineReservation { get; set; } = true;

        public decimal FixedSalary { get; set; }

        // Prim yüzdeleri
        public decimal ServiceCommissionRate { get; set; } // %
        public decimal ProductCommissionRate { get; set; } // %
        public decimal PackageCommissionRate { get; set; } // %

        public bool IsActive { get; set; } = true;


        //Finans alanları icin kullanilacak
        public SalaryPeriodType SalaryPeriod { get; set; } = SalaryPeriodType.Monthly;
        public int PaymentDay { get; set; } = 1; // Ayın kaçında ödenir
        public PaymentMethod PreferredPaymentMethod { get; set; } = PaymentMethod.BankTransfer;
        public DateOnly EmploymentStartDate { get; set; } // baslangic tarihi
        public int? PaymentWeekday { get; set; } // pzt 1 , sali 2 ...
        public DateTime? LastSalaryPaymentDate { get; set; }

        // Komisyon takibi için
        public decimal CurrentPeriodCommission { get; set; } = 0; // Mevcut dönem komisyonu
        public DateTime LastCommissionResetDate { get; set; } = DateTime.Now;


        // Navigation properties
        public ICollection<ReservationDetail> ReservationDetails { get; set; }
        public ICollection<EmployeeService> EmployeeServices { get; set; }
        public ICollection<EmployeeWorkingHours> WorkingHours { get; set; }

        //finans
        public ICollection<EmployeeCommission> Commissions { get; set; }
        public ICollection<EmployeePayment> Payments { get; set; }


    }
}
