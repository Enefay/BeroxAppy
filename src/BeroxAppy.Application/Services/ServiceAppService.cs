using BeroxAppy.Enums;
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
    public class ServiceAppService : CrudAppService<Service, ServiceDto, Guid, GetServicesInput, ServiceDto>,IServiceAppService
    {
        private readonly IRepository<ServiceCategory, Guid> _categoryRepository;

        public ServiceAppService(
            IRepository<Service, Guid> repository,
            IRepository<ServiceCategory, Guid> categoryRepository)
            : base(repository)
        {
            _categoryRepository = categoryRepository;
        }


        public override async Task<ServiceDto> CreateAsync(ServiceDto input)
        {
          
            return await base.CreateAsync(input);
        }

        /// <summary>
        /// Filtreleme ile liste getir (override)
        /// </summary>
        protected override async Task<IQueryable<Service>> CreateFilteredQueryAsync(GetServicesInput input)
        {
            var query = await ReadOnlyRepository.GetQueryableAsync();

            // Temel filtreleme
            if (!string.IsNullOrWhiteSpace(input.Filter))
            {
                query = query.Where(x =>
                    x.Title.Contains(input.Filter) ||
                    x.Description.Contains(input.Filter));
            }

            // Kategori filtresi
            if (input.CategoryId.HasValue)
            {
                query = query.Where(x => x.CategoryId == input.CategoryId);
            }

            // Cinsiyet filtresi
            if (input.TargetGender.HasValue)
            {
                query = query.Where(x => x.TargetGender == input.TargetGender || x.TargetGender == Gender.Unisex);
            }

            // Aktiflik filtresi
            if (input.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == input.IsActive);
            }

            // Fiyat aralığı
            if (input.MinPrice.HasValue)
            {
                query = query.Where(x => x.Price >= input.MinPrice);
            }
            if (input.MaxPrice.HasValue)
            {
                query = query.Where(x => x.Price <= input.MaxPrice);
            }

            // Süre aralığı
            if (input.MinDuration.HasValue)
            {
                query = query.Where(x => x.DurationMinutes >= input.MinDuration);
            }
            if (input.MaxDuration.HasValue)
            {
                query = query.Where(x => x.DurationMinutes <= input.MaxDuration);
            }

            // Default sıralama
            if (string.IsNullOrEmpty(input.Sorting))
            {
                query = query.OrderBy(x => x.Title);
            }

            return query;
        }

        /// <summary>
        /// Liste getir (override) - kategori bilgisiyle birlikte
        /// </summary>
        public override async Task<PagedResultDto<ServiceDto>> GetListAsync(GetServicesInput input)
        {
            var result = await base.GetListAsync(input);

            // Her bir DTO için kategori bilgisini ekle
            foreach (var dto in result.Items)
            {
                await EnrichServiceDtoAsync(dto);
            }

            return result;
        }

        /// <summary>
        /// Tekil getir (override) - kategori bilgisiyle birlikte  
        /// </summary>
        public override async Task<ServiceDto> GetAsync(Guid id)
        {
            var dto = await base.GetAsync(id);
            await EnrichServiceDtoAsync(dto);
            return dto;
        }

        /// <summary>
        /// ServiceDto'yu zenginleştir
        /// </summary>
        private async Task EnrichServiceDtoAsync(ServiceDto dto)
        {
            // Kategori adını ekle
            if (dto.CategoryId.HasValue)
            {
                var category = await _categoryRepository.FindAsync(dto.CategoryId.Value);
                dto.CategoryName = category?.Name;
            }

            // Display alanlarını doldur
            dto.DurationDisplay = FormatDuration(dto.DurationMinutes);
            dto.TargetGenderDisplay = GetGenderDisplayName(dto.TargetGender);
        }

        /// <summary>
        /// Aktif hizmetleri getir (dropdown için)
        /// </summary>
        public async Task<ListResultDto<ServiceDto>> GetActiveListAsync()
        {
            var queryable = await Repository.GetQueryableAsync();
            var services = queryable
                .Where(x => x.IsActive)
                .OrderBy(x => x.Title)
                .ToList();

            var dtos = ObjectMapper.Map<List<Service>, List<ServiceDto>>(services);

            // Her bir DTO'yu zenginleştir
            foreach (var dto in dtos)
            {
                await EnrichServiceDtoAsync(dto);
            }

            return new ListResultDto<ServiceDto>(dtos);
        }

        /// <summary>
        /// Kategoriye göre aktif hizmetleri getir
        /// </summary>
        public async Task<ListResultDto<ServiceDto>> GetActiveServicesByCategoryAsync(Guid categoryId)
        {
            var queryable = await Repository.GetQueryableAsync();
            var services = queryable
                .Where(x => x.IsActive && x.CategoryId == categoryId)
                .OrderBy(x => x.Title)
                .ToList();

            var dtos = ObjectMapper.Map<List<Service>, List<ServiceDto>>(services);

            // Her bir DTO'yu zenginleştir
            foreach (var dto in dtos)
            {
                await EnrichServiceDtoAsync(dto);
            }

            return new ListResultDto<ServiceDto>(dtos);
        }

        /// <summary>
        /// Cinsiyete göre aktif hizmetleri getir
        /// </summary>
        public async Task<ListResultDto<ServiceDto>> GetActiveServicesByGenderAsync(Gender targetGender)
        {
            var queryable = await Repository.GetQueryableAsync();
            var services = queryable
                .Where(x => x.IsActive && (x.TargetGender == targetGender || x.TargetGender == Gender.Unisex))
                .OrderBy(x => x.Title)
                .ToList();

            var dtos = ObjectMapper.Map<List<Service>, List<ServiceDto>>(services);

            // Her bir DTO'yu zenginleştir
            foreach (var dto in dtos)
            {
                await EnrichServiceDtoAsync(dto);
            }

            return new ListResultDto<ServiceDto>(dtos);
        }

        /// <summary>
        /// Çalışana atanmış hizmetleri getir
        /// </summary>
        public async Task<ListResultDto<ServiceDto>> GetServicesByEmployeeAsync(Guid employeeId)
        {
            // Bu metod EmployeeService repository'si gerektirir
            // Şimdilik basit implement edelim, sonra geliştirebiliriz
            var queryable = await Repository.GetQueryableAsync();
            var services = queryable
                .Where(x => x.IsActive)
                .OrderBy(x => x.Title)
                .ToList();

            var dtos = ObjectMapper.Map<List<Service>, List<ServiceDto>>(services);

            // Her bir DTO'yu zenginleştir
            foreach (var dto in dtos)
            {
                await EnrichServiceDtoAsync(dto);
            }

            return new ListResultDto<ServiceDto>(dtos);
        }

        /// <summary>
        /// Hizmeti aktif/pasif yap
        /// </summary>
        public async Task SetActiveStatusAsync(Guid id, bool isActive)
        {
            var service = await Repository.GetAsync(id);
            service.IsActive = isActive;
            await Repository.UpdateAsync(service);
        }

        /// <summary>
        /// Hizmet fiyatını güncelle
        /// </summary>
        public async Task UpdatePriceAsync(Guid id, decimal newPrice)
        {
            if (newPrice <= 0)
            {
                throw new UserFriendlyException("Fiyat 0'dan büyük olmalıdır.");
            }

            var service = await Repository.GetAsync(id);
            service.Price = newPrice;
            await Repository.UpdateAsync(service);
        }

        /// <summary>
        /// Popüler hizmetleri getir
        /// </summary>
        public async Task<ListResultDto<ServiceDto>> GetPopularServicesAsync(int count = 10)
        {
            // Bu metod ReservationDetail ile join gerektirir
            // Şimdilik basit implement edelim
            var queryable = await Repository.GetQueryableAsync();
            var services = queryable
                .Where(x => x.IsActive)
                .OrderBy(x => x.Title)
                .Take(count)
                .ToList();

            var dtos = ObjectMapper.Map<List<Service>, List<ServiceDto>>(services);

            // Her bir DTO'yu zenginleştir
            foreach (var dto in dtos)
            {
                await EnrichServiceDtoAsync(dto);
            }

            return new ListResultDto<ServiceDto>(dtos);
        }

        /// <summary>
        /// Silme işleminde özel kontrol
        /// </summary>
        public override async Task DeleteAsync(Guid id)
        {
            var queryable = await Repository.GetQueryableAsync();
            var service = queryable.FirstOrDefault(x => x.Id == id);

            if (service?.ReservationDetails?.Any() == true)
            {
                throw new UserFriendlyException("Bu hizmete ait rezervasyonlar bulunmaktadır. Hizmeti silmek yerine pasif yapabilirsiniz.");
            }

            await base.DeleteAsync(id);
        }


        /// <summary>
        /// Hizmet Arama
        /// </summary>
        public async Task<List<ServiceDto>> SearchServicesAsync(string? query, int maxResultCount = 5)
        {
            var queryable = await Repository.GetQueryableAsync();

            if (!string.IsNullOrWhiteSpace(query))
                queryable = queryable.Where(s => s.Title.Contains(query));

            var list = await AsyncExecuter.ToListAsync(
                queryable.Where(s => s.IsActive)
                    .OrderBy(s => s.Title)
                    .Take(maxResultCount)
            );

            var dtos = ObjectMapper.Map<List<Service>, List<ServiceDto>>(list);

            foreach (var dto in dtos)
            {
                await EnrichServiceDtoAsync(dto);
            }
            return dtos;
        }

        /// <summary>
        /// Süreyi formatla
        /// </summary>
        private string FormatDuration(int minutes)
        {
            if (minutes < 60)
                return $"{minutes} dakika";

            var hours = minutes / 60;
            var remainingMinutes = minutes % 60;

            if (remainingMinutes == 0)
                return $"{hours} saat";

            return $"{hours} saat {remainingMinutes} dakika";
        }

        /// <summary>
        /// Cinsiyet display adını getir
        /// </summary>
        private string GetGenderDisplayName(Gender gender)
        {
            return gender switch
            {
                Gender.Male => "Erkek",
                Gender.Female => "Kadın",
                Gender.Unisex => "Unisex",
                _ => "Bilinmiyor"
            };
        }
    }
}