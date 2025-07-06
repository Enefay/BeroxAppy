using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace BeroxAppy.Services
{
    public class ServiceCategoryAppService :
        CrudAppService<ServiceCategory, ServiceCategoryDto, Guid, PagedAndSortedResultRequestDto, ServiceCategoryDto>,
        IServiceCategoryAppService
    {
        public ServiceCategoryAppService(IRepository<ServiceCategory, Guid> repository)
            : base(repository)
        {
            // Permissions (isteğe bağlı)
            // GetPolicyName = BeroxAppyPermissions.ServiceCategories.Default;
            // GetListPolicyName = BeroxAppyPermissions.ServiceCategories.Default;
            // CreatePolicyName = BeroxAppyPermissions.ServiceCategories.Create;
            // UpdatePolicyName = BeroxAppyPermissions.ServiceCategories.Edit;
            // DeletePolicyName = BeroxAppyPermissions.ServiceCategories.Delete;
        }

        /// <summary>
        /// Filtreleme ile liste getir (override)
        /// </summary>
        protected override async Task<IQueryable<ServiceCategory>> CreateFilteredQueryAsync(PagedAndSortedResultRequestDto input)
        {
            var query = await ReadOnlyRepository.GetQueryableAsync();

            // Default sıralama
            if (string.IsNullOrEmpty(input.Sorting))
            {
                query = query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name);
            }

            return query;
        }

        /// <summary>
        /// Create işleminde özel logic
        /// </summary>
        public override async Task<ServiceCategoryDto> CreateAsync(ServiceCategoryDto input)
        {
            // DisplayOrder otomatik atama
            if (input.DisplayOrder == 0)
            {
                var queryable = await Repository.GetQueryableAsync();
                var maxOrder = queryable.Any() ? queryable.Max(x => x.DisplayOrder) : 0;
                input.DisplayOrder = maxOrder + 1;
            }

            // Color default atama
            if (string.IsNullOrWhiteSpace(input.Color))
            {
                input.Color = "#3498db"; // Default mavi
            }

            return await base.CreateAsync(input);
        }

        /// <summary>
        /// Aktif kategorileri getir (dropdown için)
        /// </summary>
        public async Task<ListResultDto<ServiceCategoryDto>> GetActiveListAsync()
        {
            var queryable = await Repository.GetQueryableAsync();
            var categories = queryable
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToList();

            return new ListResultDto<ServiceCategoryDto>(
                ObjectMapper.Map<List<ServiceCategory>, List<ServiceCategoryDto>>(categories)
            );
        }

        /// <summary>
        /// Display order'ı güncelle
        /// </summary>
        public async Task UpdateDisplayOrderAsync(Guid id, int newDisplayOrder)
        {
            var category = await Repository.GetAsync(id);
            category.DisplayOrder = newDisplayOrder;
            await Repository.UpdateAsync(category);
        }

        /// <summary>
        /// Kategoriyi aktif/pasif yap
        /// </summary>
        public async Task SetActiveStatusAsync(Guid id, bool isActive)
        {
            var category = await Repository.GetAsync(id);
            category.IsActive = isActive;
            await Repository.UpdateAsync(category);
        }

        /// <summary>
        /// Silme işleminde özel kontrol
        /// </summary>
        public override async Task DeleteAsync(Guid id)
        {
            // Bu kategoriye ait hizmet var mı kontrol et
            var queryable = await Repository.GetQueryableAsync();
            var category = queryable.FirstOrDefault(x => x.Id == id);

            if (category?.Services?.Any() == true)
            {
                throw new UserFriendlyException("Bu kategoriye ait hizmetler bulunmaktadır. Önce hizmetleri silin veya başka kategoriye taşıyın.");
            }

            await base.DeleteAsync(id);
        }
    }
}