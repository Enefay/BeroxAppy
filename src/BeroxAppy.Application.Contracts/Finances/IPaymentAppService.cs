using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace BeroxAppy.Finances
{
    public interface IPaymentAppService : IApplicationService
    {
        Task<PaymentDto> CreateAsync(CreatePaymentDto input);
        Task<PaymentDto> GetAsync(Guid id);
        Task<PagedResultDto<PaymentDto>> GetListAsync(GetPaymentsInput input);
        Task<ListResultDto<PaymentDto>> GetReservationPaymentsAsync(Guid reservationId);
        Task<ListResultDto<PaymentDto>> GetCustomerPaymentsAsync(Guid customerId);
        Task<PaymentDto> CreateReservationPaymentAsync(CreateReservationPaymentDto input);
        Task<decimal> GetReservationPaidAmountAsync(Guid reservationId);
        Task<decimal> GetReservationRemainingAmountAsync(Guid reservationId);
        Task DeleteAsync(Guid id);
    }
}
