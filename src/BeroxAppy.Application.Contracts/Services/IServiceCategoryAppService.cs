using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace BeroxAppy.Services
{
    public interface IServiceCategoryAppService :
       ICrudAppService<ServiceCategoryDto, Guid, PagedAndSortedResultRequestDto, ServiceCategoryDto>
    {
        /// <summary>
        /// Aktif kategorileri getir (dropdown için)
        /// </summary>
        Task<ListResultDto<ServiceCategoryDto>> GetActiveListAsync();

        /// <summary>
        /// Display order'ı güncelle
        /// </summary>
        Task UpdateDisplayOrderAsync(Guid id, int newDisplayOrder);

        /// <summary>
        /// Kategoriyi aktif/pasif yap
        /// </summary>
        Task SetActiveStatusAsync(Guid id, bool isActive);
    }
}
