using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace BeroxAppy.Customers
{
    public interface ICustomerAppService :
        ICrudAppService<CustomerDto, Guid, GetCustomersInput, CustomerDto>
    {

        /// <summary>
        /// Aktif müşterileri getir (dropdown için)
        /// </summary>
        Task<ListResultDto<CustomerDto>> GetActiveListAsync();

        /// <summary>
        /// Telefon numarasına göre müşteri ara
        /// </summary>
        Task<CustomerDto> GetByPhoneAsync(string phone);

        /// <summary>
        /// Müşteriyi aktif/pasif yap
        /// </summary>
        Task SetActiveStatusAsync(Guid id, bool isActive);

        /// <summary>
        /// Müşteri borcunu güncelle
        /// </summary>
        Task UpdateDebtAsync(Guid id, decimal debtAmount);

        /// <summary>
        /// İndirim oranını güncelle
        /// </summary>
        Task UpdateDiscountRateAsync(Guid id, decimal discountRate);

        /// <summary>
        /// Borcu olan müşterileri getir
        /// </summary>
        Task<ListResultDto<CustomerDto>> GetCustomersWithDebtAsync();

        /// <summary>
        /// Doğum günü olan müşterileri getir
        /// </summary>
        Task<ListResultDto<CustomerDto>> GetBirthdayCustomersAsync(DateTime date);

        /// <summary>
        /// Müşteri istatistikleri
        /// </summary>
        Task<CustomerStatsDto> GetStatsAsync();
    }
}
