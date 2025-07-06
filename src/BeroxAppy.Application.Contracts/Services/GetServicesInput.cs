using BeroxAppy.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace BeroxAppy.Services
{
    public class GetServicesInput : PagedAndSortedResultRequestDto
    {
        public string Filter { get; set; }
        public Guid? CategoryId { get; set; }
        public Gender? TargetGender { get; set; }
        public bool? IsActive { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinDuration { get; set; }
        public int? MaxDuration { get; set; }
    }
}
