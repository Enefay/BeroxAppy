using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace BeroxAppy.Employees
{
    public class EmployeeServiceAssignmentDto : EntityDto<Guid>
    {
        public Guid EmployeeId { get; set; }
        public Guid ServiceId { get; set; }
        public string ServiceTitle { get; set; }
        public string ServiceCategoryName { get; set; }
        public decimal ServicePrice { get; set; }
        public int ServiceDuration { get; set; }
    }
}
