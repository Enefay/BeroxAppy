using BeroxAppy.Customers;
using BeroxAppy.Employees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;

namespace BeroxAppy.Sells
{
    // Ürün Satış
    public class ProductSale : FullAuditedAggregateRoot<Guid>
    {
        public Guid? TenantId { get; set; }

        public Guid CustomerId { get; set; }
        public Customer Customer { get; set; }

        public Guid ProductId { get; set; }
        public Product Product { get; set; }

        public Guid? EmployeeId { get; set; }
        public Employee Employee { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }

        public DateTime SaleDate { get; set; }

        public decimal? CommissionAmount { get; set; }
    }
}
