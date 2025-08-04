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
using BeroxAppy.Finances.SalaryDtos;
using BeroxAppy.Finances;
using System.ComponentModel.DataAnnotations;

namespace BeroxAppy.Web.Pages.Finance.Salaries
{
    public class IndexModel : BeroxAppyPageModel
    {
        private readonly IFinanceAppService _financeAppService;
        private readonly IRepository<EmployeePayment, Guid> _employeePaymentRepository;
        private readonly IRepository<Employee, Guid> _employeeRepository;

        public IndexModel(
            IFinanceAppService financeAppService,
            IRepository<EmployeePayment, Guid> employeePaymentRepository,
            IRepository<Employee, Guid> employeeRepository)
        {
            _financeAppService = financeAppService;
            _employeePaymentRepository = employeePaymentRepository;
            _employeeRepository = employeeRepository;
        }

        public List<EmployeeSalarySummaryDto> EmployeeSalaries { get; set; }
        public SalaryPaymentSummaryDto Summary { get; set; }

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostPaySalaryAsync([FromBody] PaySalaryRequestDto request)
        {
            try
            {
                if (request.EmployeeSalaries?.Any() != true)
                {
                    return new JsonResult(new { success = false, message = "Ödeme yapılacak çalışan seçilmedi." });
                }

                decimal paymentLeft = request.PaymentAmount;
                int employeeCount = 0;
                decimal totalPaid = 0;

                foreach (var employeeSalary in request.EmployeeSalaries)
                {
                    if (employeeSalary.Amount > 0 && paymentLeft > 0)
                    {
                        // Çalışanın maaşından fazla ödeme olamaz!
                        decimal payThis = Math.Min(employeeSalary.Amount, paymentLeft);

                        await _financeAppService.PayEmployeeSalaryAsync(
                            employeeSalary.EmployeeId,
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
                    message = $"{employeeCount} çalışana toplam ₺{totalPaid:N2} maaş ödemesi başarıyla yapıldı.",
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
                var startDate = DateTime.Now.AddMonths(-6).Date; // Son 6 ay
                var endDate = DateTime.Now.Date;

                var performance = await _financeAppService.GetEmployeeSalaryPerformanceAsync(employeeId, startDate, endDate);

                // Son ödemeleri getir
                var recentPayments = await _employeePaymentRepository.GetListAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.PaymentType == PaymentType.Salary);

                var recentPaymentsList = recentPayments
                    .OrderByDescending(x => x.PaymentDate)
                    .Take(10)
                    .Select(x => new
                    {
                        x.Id,
                        x.PaymentDate,
                        x.SalaryAmount,
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
                            TotalSalaryPaid = performance.TotalSalaryPaid,
                            PaymentCount = performance.PaymentCount,
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
                    employeeSalaries = EmployeeSalaries,
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
            // Çalışan maaşları
            EmployeeSalaries = await _financeAppService.GetEmployeeSalariesAsync();

            // Özet bilgileri hesapla
            var totalDue = EmployeeSalaries.Where(x => x.IsDue).Sum(x => x.CalculatedAmount);
            var dueEmployeeCount = EmployeeSalaries.Count(x => x.IsDue);

            // Bu ay ödenen maaşları hesapla
            var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var thisMonthPayments = await _employeePaymentRepository.GetListAsync(x =>
                x.PaymentDate >= thisMonthStart &&
                x.PaymentType == PaymentType.Salary);

            var thisMonthPaid = thisMonthPayments.Sum(x => x.SalaryAmount);

            // Dönem özetleri
            var periodSummaries = new Dictionary<SalaryPeriodType, SalaryPeriodSummary>();

            foreach (SalaryPeriodType period in Enum.GetValues<SalaryPeriodType>())
            {
                var periodEmployees = EmployeeSalaries.Where(x => x.SalaryPeriod == period).ToList();
                periodSummaries[period] = new SalaryPeriodSummary
                {
                    EmployeeCount = periodEmployees.Count,
                    TotalAmount = periodEmployees.Sum(x => x.CalculatedAmount),
                    DueCount = periodEmployees.Count(x => x.IsDue)
                };
            }

            Summary = new SalaryPaymentSummaryDto
            {
                TotalDueSalaries = totalDue,
                DueEmployeeCount = dueEmployeeCount,
                ThisMonthPaidSalaries = thisMonthPaid,
                TotalEmployeeCount = EmployeeSalaries.Count,
                PeriodSummaries = periodSummaries
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
    public class PaySalaryRequestDto
    {
        [Required]
        public List<EmployeeSalaryDto> EmployeeSalaries { get; set; }
        [Required]
        public PaymentMethod PaymentMethod { get; set; }
        [Required]
        public decimal PaymentAmount { get; set; }
    }

    public class EmployeeSalaryDto
    {
        [Required]
        public Guid EmployeeId { get; set; }

        [Required]
        public string EmployeeName { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
    }

    public class SalaryPaymentSummaryDto
    {
        public decimal TotalDueSalaries { get; set; }
        public int DueEmployeeCount { get; set; }
        public decimal ThisMonthPaidSalaries { get; set; }
        public int TotalEmployeeCount { get; set; }
        public Dictionary<SalaryPeriodType, SalaryPeriodSummary> PeriodSummaries { get; set; }
    }

    public class SalaryPeriodSummary
    {
        public int EmployeeCount { get; set; }
        public decimal TotalAmount { get; set; }
        public int DueCount { get; set; }
    }
}