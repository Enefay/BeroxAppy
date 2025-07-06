using BeroxAppy.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace BeroxAppy.Customers
{
    public class GetCustomersInput : PagedAndSortedResultRequestDto
    {
        public string? Filter { get; set; } // Ad, telefon, email'de arama
        public Gender? Gender { get; set; }
        public bool? IsActive { get; set; }
        public bool? HasDebt { get; set; } // Borcu olanlar
        public DateTime? BirthDateFrom { get; set; }
        public DateTime? BirthDateTo { get; set; }
        public decimal? MinDiscountRate { get; set; }
        public decimal? MaxDiscountRate { get; set; }
    }
}
