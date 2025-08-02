using BeroxAppy.Finances.FinanceAppDtos;
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

        // Günlük kasa raporu
        Task<DailyCashReportDto> GetDailyCashReportAsync(DateTime date);

        // Kasa kapatma
        Task<CashRegisterDto> CloseCashRegisterAsync(Guid cashRegisterId, decimal actualClosingBalance, string note = null);
        Task<CashRegisterDto> GetTodaysCashRegisterAsync();

        // Çalışan ödemelerini getir
        Task<PagedResultDto<EmployeePaymentDto>> GetEmployeePaymentsAsync(GetEmployeePaymentsInput input);

        // Çalışan ödeme detayını getir
        Task<EmployeePaymentDto> GetEmployeePaymentAsync(Guid id);

        // Çalışan toplam ödeme tutarını getir
        Task<decimal> GetEmployeeTotalPaymentsAsync(Guid employeeId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Kapatılmış bir kasayı tekrar açar (sadece bugünkü kasa için).
        /// </summary>
        Task<CashRegisterDto> ReopenCashRegisterAsync(Guid cashRegisterId);
    }
}
