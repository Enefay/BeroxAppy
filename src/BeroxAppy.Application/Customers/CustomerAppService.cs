// CustomerAppService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BeroxAppy.Customers;
using BeroxAppy.Enums;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace BeroxAppy.Customers
{
    public class CustomerAppService :
        CrudAppService<Customer, CustomerDto, Guid, GetCustomersInput, CustomerDto>,
        ICustomerAppService
    {
        public CustomerAppService(IRepository<Customer, Guid> repository)
            : base(repository)
        {
        }

        /// <summary>
        /// Filtreleme ile liste getir (override)
        /// </summary>
        protected override async Task<IQueryable<Customer>> CreateFilteredQueryAsync(GetCustomersInput input)
        {
            var query = await ReadOnlyRepository.GetQueryableAsync();

            // Temel filtreleme (Ad, telefon, email)
            if (!string.IsNullOrWhiteSpace(input.Filter))
            {
                query = query.Where(x =>
                    x.FullName.Contains(input.Filter) ||
                    x.Phone.Contains(input.Filter) ||
                    (x.Email != null && x.Email.Contains(input.Filter)));
            }

            // Cinsiyet filtresi
            if (input.Gender.HasValue)
            {
                query = query.Where(x => x.Gender == input.Gender);
            }

            // Aktiflik filtresi
            if (input.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == input.IsActive);
            }

            // Borç filtresi
            if (input.HasDebt.HasValue)
            {
                if (input.HasDebt.Value)
                {
                    query = query.Where(x => x.TotalDebt > 0);
                }
                else
                {
                    query = query.Where(x => x.TotalDebt <= 0);
                }
            }

            // Doğum tarihi aralığı
            if (input.BirthDateFrom.HasValue)
            {
                query = query.Where(x => x.BirthDate >= input.BirthDateFrom);
            }
            if (input.BirthDateTo.HasValue)
            {
                query = query.Where(x => x.BirthDate <= input.BirthDateTo);
            }

            // İndirim oranı aralığı
            if (input.MinDiscountRate.HasValue)
            {
                query = query.Where(x => x.DiscountRate >= input.MinDiscountRate);
            }
            if (input.MaxDiscountRate.HasValue)
            {
                query = query.Where(x => x.DiscountRate <= input.MaxDiscountRate);
            }

            // Default sıralama
            if (string.IsNullOrEmpty(input.Sorting))
            {
                query = query.OrderBy(x => x.FullName);
            }

            return query;
        }

        /// <summary>
        /// Liste getir (override) - display alanlarını doldur
        /// </summary>
        public override async Task<PagedResultDto<CustomerDto>> GetListAsync(GetCustomersInput input)
        {
            var result = await base.GetListAsync(input);

            // Her bir DTO için display alanlarını doldur
            foreach (var dto in result.Items)
            {
                EnrichCustomerDto(dto);
            }

            return result;
        }

        /// <summary>
        /// Tekil getir (override) - display alanlarını doldur
        /// </summary>
        public override async Task<CustomerDto> GetAsync(Guid id)
        {
            var dto = await base.GetAsync(id);
            EnrichCustomerDto(dto);
            return dto;
        }

        /// <summary>
        /// Create işleminde özel kontroller
        /// </summary>
        public override async Task<CustomerDto> CreateAsync(CustomerDto input)
        {
            // Telefon numarası unique kontrolü
            await CheckPhoneUniquenessAsync(input.Phone);

            return await base.CreateAsync(input);
        }

        /// <summary>
        /// Update işleminde özel kontroller
        /// </summary>
        public override async Task<CustomerDto> UpdateAsync(Guid id, CustomerDto input)
        {
            // Telefon numarası unique kontrolü (kendisi hariç)
            await CheckPhoneUniquenessAsync(input.Phone, id);

            return await base.UpdateAsync(id, input);
        }

        /// <summary>
        /// Aktif müşterileri getir
        /// </summary>
        public async Task<ListResultDto<CustomerDto>> GetActiveListAsync()
        {
            var queryable = await Repository.GetQueryableAsync();
            var customers = queryable
                .Where(x => x.IsActive)
                .OrderBy(x => x.FullName)
                .ToList();

            var dtos = ObjectMapper.Map<List<Customer>, List<CustomerDto>>(customers);

            foreach (var dto in dtos)
            {
                EnrichCustomerDto(dto);
            }

            return new ListResultDto<CustomerDto>(dtos);
        }

        /// <summary>
        /// Telefon numarasına göre müşteri ara
        /// </summary>
        public async Task<CustomerDto> GetByPhoneAsync(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                throw new UserFriendlyException("Telefon numarası boş olamaz.");
            }

            var queryable = await Repository.GetQueryableAsync();
            var customer = queryable.FirstOrDefault(x => x.Phone == phone.Trim());

            if (customer == null)
            {
                throw new UserFriendlyException("Bu telefon numarasına ait müşteri bulunamadı.");
            }

            var dto = ObjectMapper.Map<Customer, CustomerDto>(customer);
            EnrichCustomerDto(dto);
            return dto;
        }

        /// <summary>
        /// Müşteriyi aktif/pasif yap
        /// </summary>
        public async Task SetActiveStatusAsync(Guid id, bool isActive)
        {
            var customer = await Repository.GetAsync(id);
            customer.IsActive = isActive;
            await Repository.UpdateAsync(customer);
        }

        /// <summary>
        /// Müşteri borcunu güncelle
        /// </summary>
        public async Task UpdateDebtAsync(Guid id, decimal debtAmount)
        {
            var customer = await Repository.GetAsync(id);
            customer.TotalDebt = debtAmount;
            await Repository.UpdateAsync(customer);
        }

        /// <summary>
        /// İndirim oranını güncelle
        /// </summary>
        public async Task UpdateDiscountRateAsync(Guid id, decimal discountRate)
        {
            if (discountRate < 0 || discountRate > 100)
            {
                throw new UserFriendlyException("İndirim oranı 0-100 arasında olmalıdır.");
            }

            var customer = await Repository.GetAsync(id);
            customer.DiscountRate = discountRate;
            await Repository.UpdateAsync(customer);
        }

        /// <summary>
        /// Borcu olan müşterileri getir
        /// </summary>
        public async Task<ListResultDto<CustomerDto>> GetCustomersWithDebtAsync()
        {
            var queryable = await Repository.GetQueryableAsync();
            var customers = queryable
                .Where(x => x.TotalDebt > 0 && x.IsActive)
                .OrderByDescending(x => x.TotalDebt)
                .ToList();

            var dtos = ObjectMapper.Map<List<Customer>, List<CustomerDto>>(customers);

            foreach (var dto in dtos)
            {
                EnrichCustomerDto(dto);
            }

            return new ListResultDto<CustomerDto>(dtos);
        }

        /// <summary>
        /// Doğum günü olan müşterileri getir
        /// </summary>
        public async Task<ListResultDto<CustomerDto>> GetBirthdayCustomersAsync(DateTime date)
        {
            var queryable = await Repository.GetQueryableAsync();
            var customers = queryable
                .Where(x => x.BirthDate.HasValue &&
                           x.BirthDate.Value.Month == date.Month &&
                           x.BirthDate.Value.Day == date.Day &&
                           x.IsActive)
                .OrderBy(x => x.FullName)
                .ToList();

            var dtos = ObjectMapper.Map<List<Customer>, List<CustomerDto>>(customers);

            foreach (var dto in dtos)
            {
                EnrichCustomerDto(dto);
            }

            return new ListResultDto<CustomerDto>(dtos);
        }

        /// <summary>
        /// Müşteri istatistikleri
        /// </summary>
        public async Task<CustomerStatsDto> GetStatsAsync()
        {
            var queryable = await Repository.GetQueryableAsync();

            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            return new CustomerStatsDto
            {
                TotalCustomers = queryable.Count(),
                ActiveCustomers = queryable.Count(x => x.IsActive),
                CustomersWithDebt = queryable.Count(x => x.TotalDebt > 0),
                TotalDebtAmount = queryable.Sum(x => x.TotalDebt),
                NewCustomersThisMonth = queryable.Count(x => x.CreationTime >= startOfMonth),
                BirthdaysThisMonth = queryable.Count(x =>
                    x.BirthDate.HasValue &&
                    x.BirthDate.Value.Month == now.Month &&
                    x.IsActive)
            };
        }

        /// <summary>
        /// Silme işleminde özel kontrol
        /// </summary>
        public override async Task DeleteAsync(Guid id)
        {
            var queryable = await Repository.GetQueryableAsync();
            var customer = queryable.FirstOrDefault(x => x.Id == id);

            if (customer?.Reservations?.Any() == true)
            {
                throw new UserFriendlyException("Bu müşteriye ait rezervasyonlar bulunmaktadır. Müşteriyi silmek yerine pasif yapabilirsiniz.");
            }

            await base.DeleteAsync(id);
        }

        /// <summary>
        /// CustomerDto'yu zenginleştir
        /// </summary>
        private void EnrichCustomerDto(CustomerDto dto)
        {
            // Cinsiyet display
            dto.GenderDisplay = dto.Gender switch
            {
                Gender.Male => "Erkek",
                Gender.Female => "Kadın",
                Gender.Unisex => "Belirtilmemiş",
                _ => "Bilinmiyor"
            };

            // Yaş hesaplama
            if (dto.BirthDate.HasValue)
            {
                var today = DateTime.Today;
                var age = today.Year - dto.BirthDate.Value.Year;
                if (dto.BirthDate.Value.Date > today.AddYears(-age))
                    age--;
                dto.Age = age;
            }

            // Borç durumu display
            if (dto.TotalDebt > 0)
            {
                dto.DebtStatusDisplay = $"₺{dto.TotalDebt:N2} Borç";
            }
            else
            {
                dto.DebtStatusDisplay = "Borcu Yok";
            }
        }

        /// <summary>
        /// Telefon numarası unique kontrolü
        /// </summary>
        private async Task CheckPhoneUniquenessAsync(string phone, Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return;

            var queryable = await Repository.GetQueryableAsync();
            var exists = queryable.Any(x => x.Phone == phone.Trim() &&
                                           (excludeId == null || x.Id != excludeId));

            if (exists)
            {
                throw new UserFriendlyException("Bu telefon numarası zaten kayıtlı.");
            }
        }
    }
}