using BeroxAppy.Employees;
using BeroxAppy.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace BeroxAppy.Reservations
{
    // Randevu Detayları - ÇALIŞAN-HİZMET EŞLEŞMESİ
    public class ReservationDetail : Entity<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }

        public Guid ReservationId { get; set; }
        public Reservation Reservation { get; set; }

        // Hangi hizmet
        public Guid ServiceId { get; set; }
        public Service Service { get; set; }

        // Hangi çalışan yapacak
        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; }

        // Bu hizmetin bu randevudaki fiyatı
        public decimal ServicePrice { get; set; }

        // Bu hizmetin başlangıç ve bitiş saati
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        // Çalışanın bu hizmetten alacağı komisyon
        public decimal CommissionRate { get; set; } // Yüzde
        public decimal CommissionAmount { get; set; } // Hesaplanan tutar

        [MaxLength(200)]
        public string? Note { get; set; }
    }
}
