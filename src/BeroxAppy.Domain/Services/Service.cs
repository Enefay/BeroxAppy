using BeroxAppy.Employees;
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

namespace BeroxAppy.Services
{
    // Hizmetler
    public class Service : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public Gender TargetGender { get; set; }

        public int DurationMinutes { get; set; }

        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;

        // Kategori ilişkisi
        public Guid? CategoryId { get; set; }
        public ServiceCategory Category { get; set; }

        // Navigation properties
        public ICollection<ReservationDetail> ReservationDetails { get; set; }
        public ICollection<EmployeeService> EmployeeServices { get; set; }
    }
}
