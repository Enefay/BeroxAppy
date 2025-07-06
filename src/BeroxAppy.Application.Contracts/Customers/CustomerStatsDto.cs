using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeroxAppy.Customers
{
    public class CustomerStatsDto
    {
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int CustomersWithDebt { get; set; }
        public decimal TotalDebtAmount { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public int BirthdaysThisMonth { get; set; }
    }
}
