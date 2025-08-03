using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BeroxAppy.Services;
using BeroxAppy.Finances.FinanceAppDtos;
using BeroxAppy.Enums;
using BeroxAppy.Employees;
using Volo.Abp.Domain.Repositories;
using BeroxAppy.Finance;
using BeroxAppy.Finances;
using System.ComponentModel.DataAnnotations;

namespace BeroxAppy.Web.Pages.Finance.Commissions
{
    public class IndexModel : BeroxAppyPageModel
    {
        private readonly IFinanceAppService _financeAppService;
        private readonly IRepository<EmployeePayment, Guid> _employeePaymentRepository;

        public IndexModel(
            IFinanceAppService financeAppService,
            IRepository<EmployeePayment, Guid> employeePaymentRepository)
        {
            _financeAppService = financeAppService;
            _employeePaymentRepository = employeePaymentRepository;
        }

        public List<EmployeeCommissionSummaryDto> EmployeeCommissions { get; set; }
        public CommissionPaymentSummaryDto Summary { get; set; }

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }


        //todo cladue bababa
        public async Task<IActionResult> OnPostPayCommissionAsync([FromBody] PayCommissionRequestDto request)
        {
            try
            {
                if (request.EmployeeCommissions?.Any() != true)
                {
                    return new JsonResult(new { success = false, message = "Ödeme yapılacak çalışan seçilmedi." });
                }

                decimal paymentLeft = request.PaymentAmount;
                int employeeCount = 0;
                decimal totalPaid = 0;

                foreach (var employeeCommission in request.EmployeeCommissions)
                {
                    if (employeeCommission.Amount > 0 && paymentLeft > 0)
                    {
                        // Çalışanın bekleyen komisyonundan fazla ödeme olamaz!
                        decimal payThis = Math.Min(employeeCommission.Amount, paymentLeft);

                        await _financeAppService.PayEmployeeCommissionAsync(
                            employeeCommission.EmployeeId,
                            payThis,
                            request.PaymentMethod
                        );

                        paymentLeft -= payThis;
                        totalPaid += payThis;
                        employeeCount++;

                        if (paymentLeft <= 0)
                            break;
                    }
                }


                return new JsonResult(new
                {
                    success = true,
                    message = $"{employeeCount} çalışana toplam ₺{totalPaid:N2} komisyon ödemesi başarıyla yapıldı.",
                    totalAmount = totalPaid,
                    employeeCount = employeeCount
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetEmployeeDetailAsync(Guid employeeId)
        {
            try
            {
                var startDate = DateTime.Now.AddMonths(-3).Date; // Son 3 ay
                var endDate = DateTime.Now.Date;

                var performance = await _financeAppService.GetEmployeePerformanceAsync(employeeId, startDate, endDate);

                // Son ödemeleri getir
                var recentPayments = await _employeePaymentRepository.GetListAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.PaymentType == PaymentType.Commission);

                var recentPaymentsList = recentPayments
                    .OrderByDescending(x => x.PaymentDate)
                    .Take(10)
                    .Select(x => new
                    {
                        x.Id,
                        x.PaymentDate,
                        x.CommissionAmount,
                        x.PaymentMethod,
                        x.Note,
                        PaymentMethodDisplay = GetPaymentMethodDisplay(x.PaymentMethod)
                    })
                    .ToList();

                return new JsonResult(new
                {
                    success = true,
                    data = new
                    {
                        Performance = performance,
                        RecentPayments = recentPaymentsList,
                        Summary = new
                        {
                            TotalEarned = performance.TotalCommissionEarned,
                            TotalPaid = performance.TotalCommissionPaid,
                            ServiceCount = performance.ServiceCount,
                            PeriodStart = startDate,
                            PeriodEnd = endDate
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostRefreshDataAsync()
        {
            try
            {
                await LoadDataAsync();
                return new JsonResult(new
                {
                    success = true,
                    message = "Veriler başarıyla yenilendi.",
                    employeeCommissions = EmployeeCommissions,
                    summary = Summary
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        private async Task LoadDataAsync()
        {
            // Çalışan komisyonları
            EmployeeCommissions = await _financeAppService.GetEmployeeCommissionsAsync();

            // Özet bilgileri hesapla
            var totalPending = EmployeeCommissions.Sum(x => x.CurrentCommission);
            var activeEmployeeCount = EmployeeCommissions.Count(x => x.CurrentCommission > 0);

            // Bu ay ödenen komisyonları hesapla
            var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var thisMonthPayments = await _employeePaymentRepository.GetListAsync(x =>
                x.PaymentDate >= thisMonthStart &&
                x.PaymentType == PaymentType.Commission);

            var thisMonthPaid = thisMonthPayments.Sum(x => x.CommissionAmount);

            Summary = new CommissionPaymentSummaryDto
            {
                TotalPendingCommissions = totalPending,
                ActiveEmployeeCount = activeEmployeeCount,
                ThisMonthPaidCommissions = thisMonthPaid,
                TotalEmployeeCount = EmployeeCommissions.Count
            };
        }

        private string GetPaymentMethodDisplay(PaymentMethod method)
        {
            return method switch
            {
                PaymentMethod.Cash => "Nakit",
                PaymentMethod.CreditCard => "Kredi Kartı",
                PaymentMethod.DebitCard => "Banka Kartı",
                PaymentMethod.BankTransfer => "Havale/EFT",
                PaymentMethod.Check => "Çek",
                PaymentMethod.Other => "Diğer",
                _ => "Bilinmiyor"
            };
        }
    }

    // Yardımcı DTO'lar
    public class PayCommissionRequestDto
    {
        [Required]
        public List<EmployeeCommissionDto> EmployeeCommissions { get; set; }
        [Required]
        public PaymentMethod PaymentMethod { get; set; }
        [Required]
        public decimal PaymentAmount { get; set; }
    }

    public class EmployeeCommissionDto
    {
        [Required]
        public Guid EmployeeId { get; set; }

        [Required]
        public string EmployeeName { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
    }

    public class CommissionPaymentSummaryDto
    {
        public decimal TotalPendingCommissions { get; set; }
        public int ActiveEmployeeCount { get; set; }
        public decimal ThisMonthPaidCommissions { get; set; }
        public int TotalEmployeeCount { get; set; }
    }
}