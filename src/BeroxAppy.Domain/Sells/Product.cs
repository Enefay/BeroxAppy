using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;

namespace BeroxAppy.Sells
{
    public class Product : FullAuditedAggregateRoot<Guid>
    {
        public Guid? TenantId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(50)]
        public string Code { get; set; }

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation property
        public ICollection<ProductSale> ProductSales { get; set; }
    }
}
