using BeroxAppy.Employees;
using BeroxAppy.Enums;
using BeroxAppy.Finance;
using BeroxAppy.Finances;
using BeroxAppy.Finances.FinanceAppDtos;
using BeroxAppy.Finances.SalaryDtos;
using BeroxAppy.Reservations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace BeroxAppy.Services
{
    public class FinanceAppService : ApplicationService, IFinanceAppService
    {
        private readonly IRepository<Employee, Guid> _employeeRepository;
        private readonly IRepository<EmployeeCommission, Guid> _commissionRepository;
        private readonly IRepository<EmployeePayment, Guid> _employeePaymentRepository;
        private readonly IRepository<Payment, Guid> _paymentRepository;
        private readonly IRepository<DailyFinancialSummary, Guid> _dailySummaryRepository;
        private readonly IRepository<Reservation, Guid> _reservationRepository;

        public FinanceAppService(
            IRepository<Employee, Guid> employeeRepository,
            IRepository<EmployeeCommission, Guid> commissionRepository,
            IRepository<EmployeePayment, Guid> employeePaymentRepository,
            IRepository<Payment, Guid> paymentRepository,
            IRepository<DailyFinancialSummary, Guid> dailySummaryRepository,
            IRepository<Reservation, Guid> reservationRepository)
        {
            _employeeRepository = employeeRepository;
            _commissionRepository = commissionRepository;
            _employeePaymentRepository = employeePaymentRepository;
            _paymentRepository = paymentRepository;
            _dailySummaryRepository = dailySummaryRepository;
            _reservationRepository = reservationRepository;
        }

        // Dashboard verileri
        public async Task<DashboardDto> GetDashboardAsync(DateTime? date = null)
        {
            var targetDate = date?.Date ?? DateTime.Now.Date;

            // Günlük gelir hesapla
            var todayPayments = await _paymentRepository.GetListAsync(x =>
                x.PaymentDate.Date == targetDate && !x.IsRefund);
            var todayIncome = todayPayments.Sum(x => x.Amount);

            // Günlük giderler (personel ödemeleri)
            var todayEmployeePayments = await _employeePaymentRepository.GetListAsync(x =>
                x.PaymentDate.Date == targetDate);
            var todayExpenses = todayEmployeePayments.Sum(x => x.TotalAmount);

            // Bekleyen komisyonlar
            var employees = await _employeeRepository.GetListAsync(x => x.IsActive);
            var totalPendingCommissions = employees.Sum(x => x.CurrentPeriodCommission);


            // MAAŞ HESAPLAMALARI
            var employeeSalaries = await GetEmployeeSalariesAsync();
            var dueSalaries = employeeSalaries.Where(x => x.IsDue).ToList();
            var urgentSalaries = dueSalaries.Where(x => x.DaysOverdue >= 7)
                .Select(x => new UrgentSalaryDto
                {
                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.EmployeeName,
                    Amount = x.CalculatedAmount,
                    DaysOverdue = x.DaysOverdue,
                    Period = x.SalaryPeriod
                }).ToList();

            var dueCountByPeriod = dueSalaries.GroupBy(x => x.SalaryPeriod)
                .ToDictionary(g => g.Key, g => g.Count());

            var thisMonthStart = new DateTime(targetDate.Year, targetDate.Month, 1);
            var thisMonthSalaryPayments = await _employeePaymentRepository.GetListAsync(x =>
                x.PaymentDate >= thisMonthStart &&
                x.PaymentType == PaymentType.Salary);

            return new DashboardDto
            {
                Date = targetDate,
                TodayIncome = todayIncome,
                TodayExpenses = todayExpenses,
                TodayProfit = todayIncome - todayExpenses,
                PendingCommissions = totalPendingCommissions,
                EmployeeCommissions = await GetEmployeeCommissionsAsync(),
                //maas
                PendingSalaries = dueSalaries.Sum(x => x.CalculatedAmount),
                DueSalaryCount = dueSalaries.Count,
                EmployeeSalaries = employeeSalaries,
                SalarySummary = new SalaryDashboardSummaryDto
                {
                    DueCountByPeriod = dueCountByPeriod,
                    TotalDueAmount = dueSalaries.Sum(x => x.CalculatedAmount),
                    TotalDueEmployees = dueSalaries.Count,
                    ThisMonthPaidSalaries = thisMonthSalaryPayments.Sum(x => x.SalaryAmount),
                    UrgentSalaries = urgentSalaries
                }
            };
        }

        // Çalışan komisyonları listesi
        public async Task<List<EmployeeCommissionSummaryDto>> GetEmployeeCommissionsAsync()
        {
            var employees = await _employeeRepository.GetListAsync(x => x.IsActive);
            var result = new List<EmployeeCommissionSummaryDto>();

            foreach (var employee in employees)
            {
                var lastPaymentList = await _employeePaymentRepository
                .GetListAsync(x => x.EmployeeId == employee.Id && x.PaymentType == PaymentType.Commission);

                var lastPayment = lastPaymentList
                    .OrderByDescending(x => x.PaymentDate)
                    .LastOrDefault();

                result.Add(new EmployeeCommissionSummaryDto
                {
                    EmployeeId = employee.Id,
                    EmployeeName = $"{employee.FirstName} {employee.LastName}",
                    CurrentCommission = employee.CurrentPeriodCommission,
                    LastPaymentDate = lastPayment?.PaymentDate,
                    CanPay = employee.CurrentPeriodCommission > 0
                });
            }

            return result.OrderByDescending(x => x.CurrentCommission).ToList();
        }

        // Komisyon öde
        public async Task PayCommissionAsync(PayCommissionDto input)
        {
            var employee = await _employeeRepository.GetAsync(input.EmployeeId);

            if (employee.CurrentPeriodCommission <= 0)
            {
                throw new UserFriendlyException("Bu çalışanın ödenecek komisyonu bulunmuyor!");
            }

            if (input.Amount > employee.CurrentPeriodCommission)
            {
                throw new UserFriendlyException("Ödeme tutarı toplam komisyondan fazla olamaz!");
            }

            // Ödeme kaydı oluştur
            var payment = new EmployeePayment
            {
                EmployeeId = input.EmployeeId,
                SalaryAmount = 0,
                CommissionAmount = input.Amount,
                BonusAmount = 0,
                TotalAmount = input.Amount,
                PaymentDate = DateTime.Now,
                PaymentMethod = input.PaymentMethod,
                Note = input.Note ?? $"Komisyon ödemesi - {DateTime.Now:dd.MM.yyyy}",
                PeriodStart = employee.LastCommissionResetDate,
                PeriodEnd = DateTime.Now,
                PaymentType = PaymentType.Commission
            };

            await _employeePaymentRepository.InsertAsync(payment);

            // Çalışanın komisyonunu düş
            employee.CurrentPeriodCommission -= input.Amount;

            // Eğer tam ödeme yapıldıysa komisyonları "ödendi" olarak işaretle
            if (input.Amount == employee.CurrentPeriodCommission + input.Amount)
            {
                var unpaidCommissions = await _commissionRepository.GetListAsync(x =>
                    x.EmployeeId == input.EmployeeId && !x.IsPaid);

                foreach (var commission in unpaidCommissions)
                {
                    commission.IsPaid = true;
                    commission.PaidDate = DateTime.Now;
                    await _commissionRepository.UpdateAsync(commission);
                }

                employee.LastCommissionResetDate = DateTime.Now;
            }

            await _employeeRepository.UpdateAsync(employee);
        }

        // Çalışan performans raporu
        public async Task<EmployeePerformanceDto> GetEmployeePerformanceAsync(Guid employeeId, DateTime startDate, DateTime endDate)
        {
            endDate = endDate.Date.AddDays(1).AddTicks(-1);
            var employee = await _employeeRepository.GetAsync(employeeId);

            var commissions = await _commissionRepository.GetListAsync(x =>
                x.EmployeeId == employeeId &&
                x.EarnedDate >= startDate &&
                x.EarnedDate <= endDate);

            var payments = await _employeePaymentRepository.GetListAsync(x =>
                x.EmployeeId == employeeId &&
                x.PaymentDate >= startDate &&
                x.PaymentDate <= endDate);

            return new EmployeePerformanceDto
            {
                EmployeeId = employeeId,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                PeriodStart = startDate,
                PeriodEnd = endDate,
                TotalCommissionEarned = commissions.Sum(x => x.Amount),
                TotalCommissionPaid = payments.Where(x => x.PaymentType == PaymentType.Commission).Sum(x => x.CommissionAmount),
                TotalSalaryPaid = payments.Where(x => x.PaymentType == PaymentType.Salary).Sum(x => x.SalaryAmount),
                ServiceCount = commissions.Count(x => x.Type == CommissionType.Service),
                Commissions = commissions.Select(x => new CommissionDetailDto
                {
                    Amount = x.Amount,
                    Date = x.EarnedDate,
                    Description = x.Description,
                    IsPaid = x.IsPaid
                }).ToList()
            };
        }

        // Günlük özet getir veya oluştur
        public async Task<DailyFinancialSummaryDto> GetOrCreateDailySummaryAsync(DateTime date)
        {
            var targetDate = date.Date;

            var summary = await _dailySummaryRepository.FindAsync(x => x.Date == targetDate);

            if (summary == null)
            {
                summary = await CreateDailySummaryAsync(targetDate);
            }
            else
            {

                // Gün kapatılmamışsa güncel verileri hesapla ve güncelle
                // Kapatılmış günler için veriyi koruma
                if (!summary.IsClosed) //todo kontrol
                {
                    await UpdateDailySummaryAsync(summary);
                }
            }

            return ObjectMapper.Map<DailyFinancialSummary, DailyFinancialSummaryDto>(summary);
        }


        // Günü kapat
        public async Task<DailyFinancialSummaryDto> CloseDayAsync(CloseDayDto input)
        {
            var summary = await _dailySummaryRepository.FindAsync(x => x.Date == input.Date.Date);

            if (summary == null)
            {
                summary = await CreateDailySummaryAsync(input.Date.Date);
            }

            if (summary.IsClosed)
            {
                throw new UserFriendlyException("Bu gün zaten kapatılmış!");
            }

            // Son hesaplamaları yap
            await CalculateDailySummaryAsync(summary);

            // Günü kapat
            summary.IsClosed = true;
            summary.ClosedDate = DateTime.Now;
            summary.ClosedByUserId = CurrentUser.Id;
            summary.Note = input.Note;

            await _dailySummaryRepository.UpdateAsync(summary);

            return ObjectMapper.Map<DailyFinancialSummary, DailyFinancialSummaryDto>(summary);
        }

        // Haftalık/Aylık rapor
        public async Task<List<DailyFinancialSummaryDto>> GetPeriodSummaryAsync(DateTime startDate, DateTime endDate)
        {
            var summaries = await _dailySummaryRepository.GetListAsync(x =>
                x.Date >= startDate.Date && x.Date <= endDate.Date);

            return ObjectMapper.Map<List<DailyFinancialSummary>, List<DailyFinancialSummaryDto>>(
                summaries.OrderBy(x => x.Date).ToList());
        }

        //komisyon ödeme metodu:
        public async Task PayEmployeeCommissionAsync(Guid employeeId, decimal amount, PaymentMethod paymentMethod, string? note = null)
        {
            var employee = await _employeeRepository.GetAsync(employeeId);

            if (employee.CurrentPeriodCommission <= 0)
            {
                throw new UserFriendlyException("Bu çalışanın ödenecek komisyonu yok!");
            }

            if (amount > employee.CurrentPeriodCommission)
            {
                throw new UserFriendlyException("Ödeme tutarı toplam komisyondan fazla!");
            }


     


            var isPartial = amount < employee.CurrentPeriodCommission + amount; // Ödenen, toplamdan azsa kısmi

            // Ödeme kaydı
            var payment = new EmployeePayment
            {
                EmployeeId = employeeId,
                SalaryAmount = 0,
                CommissionAmount = amount,
                BonusAmount = 0,
                TotalAmount = amount,
                PaymentDate = DateTime.Now,
                PaymentMethod = paymentMethod,
                Note = string.IsNullOrWhiteSpace(note)
                ? (isPartial
                    ? $"Kısmi komisyon ödemesi - {DateTime.Now:dd.MM.yyyy}"
                    : $"Komisyon ödemesi - {DateTime.Now:dd.MM.yyyy}")
                : note,
                PeriodStart = employee.LastCommissionResetDate,
                PeriodEnd = DateTime.Now,
                PaymentType = PaymentType.Commission
            };

            await _employeePaymentRepository.InsertAsync(payment);


            // Komisyonu düş
            employee.CurrentPeriodCommission -= amount;

            // Tam ödeme yapıldıysa komisyonları "ödendi" yap
            if (employee.CurrentPeriodCommission <= 0)
            {
                var unpaidCommissions = await _commissionRepository.GetListAsync(x =>
                    x.EmployeeId == employeeId && !x.IsPaid);

                foreach (var commission in unpaidCommissions)
                {
                    commission.IsPaid = true;
                    commission.PaidDate = DateTime.Now;
                    await _commissionRepository.UpdateAsync(commission);
                }

                employee.LastCommissionResetDate = DateTime.Now;
                employee.CurrentPeriodCommission = 0;
            }

            await _employeeRepository.UpdateAsync(employee);
        }


        public async Task<List<EmployeeSalarySummaryDto>> GetEmployeeSalariesAsync()
        {
            var employees = await _employeeRepository.GetListAsync(x => x.IsActive);
            var result = new List<EmployeeSalarySummaryDto>();

            foreach (var employee in employees)
            {
                // Maaş almayanları atla
                if (employee.FixedSalary <= 0)
                {
                    continue;
                }

                // Bu çalışanın tüm maaş ödemelerini getir
                var allSalaryPayments = await _employeePaymentRepository
                    .GetListAsync(x => x.EmployeeId == employee.Id && x.PaymentType == PaymentType.Salary);

                var startDate = employee.EmploymentStartDate.ToDateTime(TimeOnly.MinValue);
                var today = DateTime.Now.Date;

                // Son ödeme tarihini bul
                var lastPayment = allSalaryPayments
                    .OrderByDescending(x => x.PaymentDate)
                    .FirstOrDefault();

                DateTime nextPeriodStart;

                if (lastPayment != null)
                {
                    // Son ödemeden sonraki dönem
                    nextPeriodStart = lastPayment.PeriodEnd.AddDays(1);
                }
                else
                {
                    // İlk maaş - işe başlama tarihinden
                    nextPeriodStart = startDate;
                }

                // Sadece bugüne kadar ödenmemiş dönemleri hesapla (max 24 dönem - 2 yıl)
                var unpaidSalaries = CalculateUnpaidSalaries(employee, nextPeriodStart, today);

                foreach (var salary in unpaidSalaries)
                {
                    var daysOverdue = (today - salary.PaymentDueDate.Date).Days;
                    var isDue = today >= salary.PaymentDueDate.Date;

                    result.Add(new EmployeeSalarySummaryDto
                    {
                        EmployeeId = employee.Id,
                        EmployeeName = $"{employee.FirstName} {employee.LastName}",
                        FixedSalary = employee.FixedSalary,
                        SalaryPeriod = employee.SalaryPeriod,
                        PaymentDay = employee.PaymentDay,
                        PaymentWeekday = employee.PaymentWeekday,
                        LastSalaryPaymentDate = lastPayment?.PaymentDate,
                        NextPaymentDue = salary.PaymentDueDate,
                        IsDue = isDue,
                        CanPay = isDue || (today >= salary.PeriodEnd.Date),
                        DaysOverdue = Math.Max(0, daysOverdue),
                        PreferredPaymentMethod = employee.PreferredPaymentMethod,
                        SalaryPeriodDisplay = GetSalaryPeriodDisplay(employee.SalaryPeriod),
                        CalculatedAmount = employee.FixedSalary,
                        PeriodStart = salary.PeriodStart,
                        PeriodEnd = salary.PeriodEnd,
                        PeriodDisplay = $"{salary.PeriodStart:dd.MM.yyyy} - {salary.PeriodEnd:dd.MM.yyyy}"
                    });
                }
            }

            return result.OrderByDescending(x => x.IsDue)
                         .ThenByDescending(x => x.DaysOverdue)
                         .ThenBy(x => x.NextPaymentDue)
                         .ToList();
        }

        // Maaş öde
        public async Task PayEmployeeSalaryAsync(Guid employeeId, decimal amount, PaymentMethod paymentMethod, string? note = null, DateTime? periodStart = null, DateTime? periodEnd = null)
        {
            var employee = await _employeeRepository.GetAsync(employeeId);

            if (amount <= 0)
            {
                throw new UserFriendlyException("Ödeme tutarı 0'dan büyük olmalı!");
            }

            // Tam maaştan fazla ödeme kontrolü
            if (amount > employee.FixedSalary)
            {
                throw new UserFriendlyException($"Ödeme tutarı maaştan (₺{employee.FixedSalary:N2}) fazla olamaz!");
            }

            // Eğer dönem bilgisi verilmemişse, otomatik hesapla
            if (!periodStart.HasValue || !periodEnd.HasValue)
            {
                var lastPayment = await _employeePaymentRepository
                    .GetListAsync(x => x.EmployeeId == employeeId && x.PaymentType == PaymentType.Salary);

                var lastSalaryPayment = lastPayment.OrderByDescending(x => x.PaymentDate).FirstOrDefault();

                if (lastSalaryPayment != null)
                {
                    periodStart = lastSalaryPayment.PeriodEnd.AddDays(1);
                }
                else
                {
                    // İlk ödeme - işe başlama tarihinden başla
                    periodStart = employee.CreationTime.Date;
                }

                periodEnd = CalculatePeriodEndFromStart(periodStart.Value, employee.SalaryPeriod);
            }

            // Ödeme kaydı oluştur
            var payment = new EmployeePayment
            {
                EmployeeId = employeeId,
                SalaryAmount = amount,
                CommissionAmount = 0,
                BonusAmount = 0,
                TotalAmount = amount,
                PaymentDate = DateTime.Now,
                PaymentMethod = paymentMethod,
                Note = string.IsNullOrWhiteSpace(note)
                    ? $"Maaş ödemesi ({periodStart.Value:dd.MM.yyyy} - {periodEnd.Value:dd.MM.yyyy})"
                    : note,
                PeriodStart = periodStart.Value,
                PeriodEnd = periodEnd.Value,
                PaymentType = PaymentType.Salary
            };

            await _employeePaymentRepository.InsertAsync(payment);

            // Çalışanın son maaş ödeme tarihini güncelle
            employee.LastSalaryPaymentDate = DateTime.Now;
            await _employeeRepository.UpdateAsync(employee);
        }

        // Çalışan maaş performansı
        public async Task<EmployeeSalaryPerformanceDto> GetEmployeeSalaryPerformanceAsync(Guid employeeId, DateTime startDate, DateTime endDate)
        {
            var employee = await _employeeRepository.GetAsync(employeeId);

            var payments = await _employeePaymentRepository.GetListAsync(x =>
                x.EmployeeId == employeeId &&
                x.PaymentDate >= startDate &&
                x.PaymentDate <= endDate);

            var salaryPayments = payments.Where(x => x.PaymentType == PaymentType.Salary).ToList();
            var commissionPayments = payments.Where(x => x.PaymentType == PaymentType.Commission).ToList();
            var bonusPayments = payments.Where(x => x.PaymentType == PaymentType.Bonus).ToList();

            var paymentHistory = salaryPayments.Select(x => new SalaryPaymentDetailDto
            {
                Amount = x.SalaryAmount,
                PaymentDate = x.PaymentDate,
                PaymentMethod = x.PaymentMethod,
                PaymentMethodDisplay = GetPaymentMethodDisplay(x.PaymentMethod),
                Note = x.Note,
                PeriodStart = x.PeriodStart,
                PeriodEnd = x.PeriodEnd
            }).OrderByDescending(x => x.PaymentDate).ToList();

            return new EmployeeSalaryPerformanceDto
            {
                EmployeeId = employeeId,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                PeriodStart = startDate,
                PeriodEnd = endDate,
                TotalSalaryPaid = salaryPayments.Sum(x => x.SalaryAmount),
                TotalCommissionPaid = commissionPayments.Sum(x => x.CommissionAmount),
                TotalBonusPaid = bonusPayments.Sum(x => x.BonusAmount),
                GrandTotal = payments.Sum(x => x.TotalAmount),
                PaymentCount = salaryPayments.Count,
                PaymentHistory = paymentHistory
            };
        }



        // Beklenen maaş dönemlerini hesapla
        private List<SalaryPeriodInfo> CalculateUnpaidSalaries(Employee employee, DateTime startDate, DateTime endDate)
        {
            var periods = new List<SalaryPeriodInfo>();
            var currentDate = startDate.Date;

            // Maximum 24 dönem hesapla (güvenlik için)
            var maxPeriods = 24;
            var periodCount = 0;

            switch (employee.SalaryPeriod)
            {
                case SalaryPeriodType.Daily:
                    // Günlük: her gün
                    while (currentDate <= endDate && periodCount < maxPeriods)
                    {
                        periods.Add(new SalaryPeriodInfo
                        {
                            PeriodStart = currentDate,
                            PeriodEnd = currentDate,
                            PaymentDueDate = currentDate.AddDays(1) // Ertesi gün öde
                        });
                        currentDate = currentDate.AddDays(1);
                        periodCount++;
                    }
                    break;

                case SalaryPeriodType.Weekly:
                    // Haftalık: belirlenen günde ödeme
                    var paymentDayOfWeek = (DayOfWeek)((employee.PaymentWeekday ?? 1) % 7); // 1=Pazartesi

                    while (currentDate <= endDate && periodCount < maxPeriods)
                    {
                        // Bu haftanın başlangıcını bul (Pazartesi)
                        var weekStart = currentDate;
                        while (weekStart.DayOfWeek != DayOfWeek.Monday)
                        {
                            weekStart = weekStart.AddDays(-1);
                        }

                        var weekEnd = weekStart.AddDays(6); // Pazar

                        // Ödeme tarihi: belirlenen günde
                        var paymentDate = weekStart;
                        while (paymentDate.DayOfWeek != paymentDayOfWeek)
                        {
                            paymentDate = paymentDate.AddDays(1);
                        }
                        // Eğer ödeme günü haftanın sonundaysa, bir sonraki haftaya kaydir
                        if (paymentDate < weekEnd)
                        {
                            paymentDate = paymentDate.AddDays(7);
                        }

                        periods.Add(new SalaryPeriodInfo
                        {
                            PeriodStart = weekStart,
                            PeriodEnd = weekEnd,
                            PaymentDueDate = paymentDate
                        });

                        currentDate = weekEnd.AddDays(1);
                        periodCount++;
                    }
                    break;

                case SalaryPeriodType.BiWeekly:
                    // 2 Haftalık
                    while (currentDate <= endDate && periodCount < maxPeriods)
                    {
                        var periodStart = currentDate;
                        var periodEnd = currentDate.AddDays(13); // 14 gün
                        var paymentDate = periodEnd.AddDays(1);

                        periods.Add(new SalaryPeriodInfo
                        {
                            PeriodStart = periodStart,
                            PeriodEnd = periodEnd,
                            PaymentDueDate = paymentDate
                        });

                        currentDate = periodEnd.AddDays(1);
                        periodCount++;
                    }
                    break;

                case SalaryPeriodType.Monthly:
                    // Aylık: ayın belirlenen günü
                    while (currentDate <= endDate && periodCount < maxPeriods)
                    {
                        var monthStart = new DateTime(currentDate.Year, currentDate.Month, 1);
                        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                        // İlk dönem: işe başladığı günden başlayabilir
                        if (periodCount == 0 && currentDate > monthStart)
                        {
                            monthStart = currentDate;
                        }

                        // Ödeme günü: ayın belirlenen günü
                        var paymentDay = Math.Min(employee.PaymentDay, DateTime.DaysInMonth(monthEnd.Year, monthEnd.Month));
                        var paymentDate = new DateTime(monthEnd.Year, monthEnd.Month, paymentDay);

                        // Eğer ödeme günü henüz geçmemişse, sonraki aya ertelenebilir
                        if (paymentDate < monthEnd)
                        {
                            paymentDate = paymentDate.AddMonths(1);
                            paymentDate = new DateTime(paymentDate.Year, paymentDate.Month,
                                Math.Min(employee.PaymentDay, DateTime.DaysInMonth(paymentDate.Year, paymentDate.Month)));
                        }

                        periods.Add(new SalaryPeriodInfo
                        {
                            PeriodStart = monthStart,
                            PeriodEnd = monthEnd,
                            PaymentDueDate = paymentDate
                        });

                        currentDate = monthEnd.AddDays(1);
                        periodCount++;
                    }
                    break;
            }

            return periods;
        }

        // Günlük özet oluştur
        private async Task<DailyFinancialSummary> CreateDailySummaryAsync(DateTime date)
        {
            var summary = new DailyFinancialSummary
            {
                Date = date.Date,
                IsClosed = false
            };

            // İlk oluşturulduğunda hesaplamaları yap
            await CalculateDailySummaryAsync(summary);
            // Veritabanına kaydet
            await _dailySummaryRepository.InsertAsync(summary);

            return summary;
        }

        // Günlük özeti güncelle
        private async Task UpdateDailySummaryAsync(DailyFinancialSummary summary)
        {
            if (summary.IsClosed) return; // Kapalı günler güncellenmez

            // Güncel hesaplamaları yap
            await CalculateDailySummaryAsync(summary);
            await _dailySummaryRepository.UpdateAsync(summary);
        }

        // Günlük özet hesapla
        private async Task CalculateDailySummaryAsync(DailyFinancialSummary summary)
        {
            var date = summary.Date;

            // GELİRLER - İade edilmeyenler
            var payments = await _paymentRepository.GetListAsync(x =>
                x.PaymentDate.Date == date && !x.IsRefund);

            // İADELER - Ayrı hesapla
            var refunds = await _paymentRepository.GetListAsync(x =>
                x.PaymentDate.Date == date && x.IsRefund);

            // Net hizmet geliri (gelir - iade)
            var grossServiceIncome = payments.Where(x => x.ReservationId.HasValue).Sum(x => x.Amount);
            var serviceRefunds = refunds.Where(x => x.ReservationId.HasValue).Sum(x => x.Amount);
            summary.ServiceIncome = Math.Max(0, grossServiceIncome - serviceRefunds);

            // Diğer gelirler (rezervasyon dışı ödemeler)
            var otherIncomePayments = payments.Where(x => !x.ReservationId.HasValue).Sum(x => x.Amount);
            var otherRefunds = refunds.Where(x => !x.ReservationId.HasValue).Sum(x => x.Amount);
            summary.OtherIncome = Math.Max(0, otherIncomePayments - otherRefunds);

            // Ürün gelirleri (şimdilik 0, ileride ürün satışları için)
            summary.ProductIncome = 0;

            // Toplam gelir
            summary.TotalIncome = summary.ServiceIncome + summary.ProductIncome + summary.OtherIncome;

            // GİDERLER
            var employeePayments = await _employeePaymentRepository.GetListAsync(x =>
                x.PaymentDate.Date == date);
            summary.EmployeePayments = employeePayments.Sum(x => x.TotalAmount);

            // Diğer giderler ve operasyonel giderler şimdilik aynı (manuel girişse 0 kalır)
            // İleride ayrı gider tablosu eklerseniz buraya ekleyebilirsiniz
            summary.TotalExpenses = summary.EmployeePayments + summary.OperationalExpenses + summary.OtherExpenses;

            // NET KAR/ZARAR
            summary.NetProfit = summary.TotalIncome - summary.TotalExpenses;

            // ÖDEME YÖNTEMLERİ DAĞILIMI (sadece net ödemeler, iadeler düşülmüş)
            var allNetPayments = payments.Concat(refunds.Select(r => new Payment
            {
                PaymentMethod = r.PaymentMethod,
                Amount = -r.Amount // İadeleri negatif yap
            }));

            summary.CashAmount = allNetPayments.Where(x => x.PaymentMethod == PaymentMethod.Cash).Sum(x => x.Amount);
            summary.CreditCardAmount = allNetPayments.Where(x => x.PaymentMethod == PaymentMethod.CreditCard).Sum(x => x.Amount);
            summary.DebitCardAmount = allNetPayments.Where(x => x.PaymentMethod == PaymentMethod.DebitCard).Sum(x => x.Amount);
            summary.BankTransferAmount = allNetPayments.Where(x => x.PaymentMethod == PaymentMethod.BankTransfer).Sum(x => x.Amount);

            // İSTATİSTİKLER
            var reservations = await _reservationRepository.GetListAsync(x => x.ReservationDate.Date == date);
            summary.TotalReservations = reservations.Count;
            summary.CompletedReservations = reservations.Count(x => x.Status == ReservationStatus.Arrived);
            summary.CancelledReservations = reservations.Count(x => x.Status == ReservationStatus.NoShow);

            // Ortalama işlem tutarı (tamamlanan rezervasyonlara göre)
            summary.AverageTicketSize = summary.CompletedReservations > 0
                ? summary.ServiceIncome / summary.CompletedReservations
                : 0;
        }


        //maas

        // Yardımcı metodlar
        private DateTime CalculateNextPaymentDate(Employee employee, DateTime? lastPaymentDate)
        {
            var baseDate = lastPaymentDate ?? DateTime.Now.AddDays(-30); // İlk ödeme için geçmiş tarih

            return employee.SalaryPeriod switch
            {
                SalaryPeriodType.Daily => baseDate.AddDays(1),
                SalaryPeriodType.Weekly => baseDate.AddDays(7),
                SalaryPeriodType.BiWeekly => baseDate.AddDays(14),
                SalaryPeriodType.Monthly => baseDate.AddMonths(1),
                _ => baseDate.AddMonths(1)
            };
        }

        private decimal CalculatePeriodAmount(decimal fixedSalary, SalaryPeriodType period)
        {
            return period switch
            {
                SalaryPeriodType.Daily => fixedSalary / 30, // Aylık maaşı 30'a böl
                SalaryPeriodType.Weekly => fixedSalary / 4, // Aylık maaşı 4'e böl
                SalaryPeriodType.BiWeekly => fixedSalary / 2, // Aylık maaşı 2'ye böl
                SalaryPeriodType.Monthly => fixedSalary, // Tam maaş
                _ => fixedSalary
            };
        }

        private bool AllowEarlyPayment(Employee employee, DateTime? lastPaymentDate)
        {
            // Erken ödeme politikası - son ödemeden en az 1 gün geçmişse ödeme yapılabilir
            if (lastPaymentDate == null) return true;

            return (DateTime.Now.Date - lastPaymentDate.Value.Date).Days >= 1;
        }

        private DateTime CalculatePeriodStart(SalaryPeriodType period, int paymentDay)
        {
            var now = DateTime.Now;

            return period switch
            {
                SalaryPeriodType.Daily => now.Date,
                SalaryPeriodType.Weekly => now.Date.AddDays(-(int)now.DayOfWeek),
                SalaryPeriodType.BiWeekly => now.Date.AddDays(-14),
                SalaryPeriodType.Monthly => new DateTime(now.Year, now.Month, 1),
                _ => new DateTime(now.Year, now.Month, 1)
            };
        }

        private DateTime CalculatePeriodEnd(DateTime periodStart, SalaryPeriodType period)
        {
            return period switch
            {
                SalaryPeriodType.Daily => periodStart,
                SalaryPeriodType.Weekly => periodStart.AddDays(6),
                SalaryPeriodType.BiWeekly => periodStart.AddDays(13),
                SalaryPeriodType.Monthly => periodStart.AddMonths(1).AddDays(-1),
                _ => periodStart.AddMonths(1).AddDays(-1)
            };
        }

        private string GetSalaryPeriodDisplay(SalaryPeriodType period)
        {
            return period switch
            {
                SalaryPeriodType.Daily => "Günlük",
                SalaryPeriodType.Weekly => "Haftalık",
                SalaryPeriodType.BiWeekly => "2 Haftalık",
                SalaryPeriodType.Monthly => "Aylık",
                _ => "Bilinmiyor"
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

        //todo settinmanager
        private DateTime CalculatePeriodEndFromStart(DateTime start, SalaryPeriodType period)
        {
            return period switch
            {
                SalaryPeriodType.Daily => start,
                SalaryPeriodType.Weekly => start.AddDays(6),
                SalaryPeriodType.BiWeekly => start.AddDays(13),
                SalaryPeriodType.Monthly => start.AddMonths(1).AddDays(-1),
                _ => start.AddMonths(1).AddDays(-1)
            };
        }
    }
}
