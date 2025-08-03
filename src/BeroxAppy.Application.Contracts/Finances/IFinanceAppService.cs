﻿using BeroxAppy.Enums;
using BeroxAppy.Finances.FinanceAppDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace BeroxAppy.Finances
{
    public interface IFinanceAppService : IApplicationService
    {
        Task<DashboardDto> GetDashboardAsync(DateTime? date = null);
        Task<List<EmployeeCommissionSummaryDto>> GetEmployeeCommissionsAsync();
        Task PayCommissionAsync(PayCommissionDto input);
        Task<EmployeePerformanceDto> GetEmployeePerformanceAsync(Guid employeeId, DateTime startDate, DateTime endDate);
        Task<DailyFinancialSummaryDto> GetOrCreateDailySummaryAsync(DateTime date);
        Task<DailyFinancialSummaryDto> CloseDayAsync(CloseDayDto input);
        Task<List<DailyFinancialSummaryDto>> GetPeriodSummaryAsync(DateTime startDate, DateTime endDate);
        Task PayEmployeeCommissionAsync(Guid employeeId, decimal amount, PaymentMethod paymentMethod, string? note = null);



        // Maaş ödeme metodları
 
    }
}
