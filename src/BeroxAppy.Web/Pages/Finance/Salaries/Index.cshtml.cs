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

                foreach (var employeeSalary in request.EmployeeSalaries)
                {
                    if (employeeSalary.Amount > 0)
                    {
                        await _financeAppService.PayEmployeeSalaryAsync(
                            employeeSalary.EmployeeId,
                            employeeSalary.Amount,
                            request.PaymentMethod,
                            request.Note
                        );
                    }
                }

                var totalAmount = request.EmployeeSalaries.Sum(x => x.Amount);
                var employeeCount = request.EmployeeSalaries.Count(x => x.Amount > 0);

                return new JsonResult(new
                {
                    success = true,
                    message = $"{employeeCount} çalışana toplam ₺{totalAmount:N2} maaş ödemesi başarıyla yapıldı.",
                    totalAmount = totalAmount,
                    employeeCount = employeeCount
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetEmployeeSalaryDetailAsync(Guid employeeId)
        {
            try
            {
                var startDate = DateTime.Now.AddMonths(-6).Date; // Son 6 ay
                var endDate = DateTime.Now.Date;

                var performance = await _financeAppService.GetEmployeeSalaryPerformanceAsync(employeeId, startDate, endDate);

                return new JsonResult(new
                {
                    success = true,
                    data = performance
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
    }
}