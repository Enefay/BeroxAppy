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
    // Hizmet Kategorileri
    public class ServiceCategory : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(50)]
        public string Color { get; set; } // Hex color code

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation property
        public ICollection<Service> Services { get; set; }
    }
}
