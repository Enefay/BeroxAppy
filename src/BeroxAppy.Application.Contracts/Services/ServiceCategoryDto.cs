using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace BeroxAppy.Services
{
    public class ServiceCategoryDto : FullAuditedEntityDto<Guid>
    {
        public string Name { get; set; }
        public string Color { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
