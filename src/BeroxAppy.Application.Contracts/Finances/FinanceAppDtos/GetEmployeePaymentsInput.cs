using BeroxAppy.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace BeroxAppy.Finances.FinanceAppDtos
{
    public class GetEmployeePaymentsInput : PagedAndSortedResultRequestDto
    {
        public Guid? EmployeeId { get; set; }
        public PaymentType? PaymentType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
