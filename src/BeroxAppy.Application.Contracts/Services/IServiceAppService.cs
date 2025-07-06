using System;
using System.Threading.Tasks;
using BeroxAppy.Enums;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace BeroxAppy.Services
{
    public interface IServiceAppService :
        ICrudAppService<ServiceDto, Guid, GetServicesInput, ServiceDto>
    {
        /// <summary>
        /// Aktif hizmetleri getir (dropdown için)
        /// </summary>
        Task<ListResultDto<ServiceDto>> GetActiveListAsync();

        /// <summary>
        /// Kategoriye göre aktif hizmetleri getir
        /// </summary>
        Task<ListResultDto<ServiceDto>> GetActiveServicesByCategoryAsync(Guid categoryId);

        /// <summary>
        /// Cinsiyete göre aktif hizmetleri getir
        /// </summary>
        Task<ListResultDto<ServiceDto>> GetActiveServicesByGenderAsync(Gender targetGender);

        /// <summary>
        /// Çalışana atanmış hizmetleri getir
        /// </summary>
        Task<ListResultDto<ServiceDto>> GetServicesByEmployeeAsync(Guid employeeId);

        /// <summary>
        /// Hizmeti aktif/pasif yap
        /// </summary>
        Task SetActiveStatusAsync(Guid id, bool isActive);

        /// <summary>
        /// Hizmet fiyatını güncelle
        /// </summary>
        Task UpdatePriceAsync(Guid id, decimal newPrice);

        /// <summary>
        /// Popüler hizmetleri getir (rezervasyon sayısına göre)
        /// </summary>
        Task<ListResultDto<ServiceDto>> GetPopularServicesAsync(int count = 10);
    }
}