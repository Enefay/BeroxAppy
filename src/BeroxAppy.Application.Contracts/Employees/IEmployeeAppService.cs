// IEmployeeAppService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BeroxAppy.Enums;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace BeroxAppy.Employees
{
    public interface IEmployeeAppService :
        ICrudAppService<EmployeeDto, Guid, GetEmployeesInput, EmployeeDto>
    {
        /// <summary>
        /// Aktif çalışanları getir (dropdown için)
        /// </summary>
        Task<ListResultDto<EmployeeDto>> GetActiveListAsync();

        /// <summary>
        /// Çalışanı aktif/pasif yap
        /// </summary>
        Task SetActiveStatusAsync(Guid id, bool isActive);

        /// <summary>
        /// Çalışan maaşını güncelle
        /// </summary>
        Task UpdateSalaryAsync(Guid id, decimal newSalary);

        /// <summary>
        /// Komisyon oranlarını güncelle
        /// </summary>
        Task UpdateCommissionRatesAsync(Guid id, decimal serviceRate, decimal productRate, decimal packageRate);

        /// <summary>
        /// Çalışan için ABP kullanıcısı oluştur
        /// </summary>
        Task CreateUserAsync(Guid employeeId, string userName, string password);

        /// <summary>
        /// Çalışan kullanıcısını aktif/pasif yap
        /// </summary>
        Task SetUserActiveStatusAsync(Guid employeeId, bool isActive);

        /// <summary>
        /// Kullanıcı şifresini sıfırla
        /// </summary>
        Task ResetUserPasswordAsync(Guid employeeId, string newPassword);

        // =============== HİZMET YÖNETİMİ ===============

        /// <summary>
        /// Çalışana hizmet ata
        /// </summary>
        Task AssignServiceAsync(Guid employeeId, Guid serviceId);

        /// <summary>
        /// Çalışandan hizmet kaldır
        /// </summary>
        Task UnassignServiceAsync(Guid employeeId, Guid serviceId);

        /// <summary>
        /// Çalışanın hizmetlerini getir
        /// </summary>
        Task<ListResultDto<EmployeeServiceAssignmentDto>> GetEmployeeServicesAsync(Guid employeeId);

        /// <summary>
        /// Hizmeti verebilen çalışanları getir
        /// </summary>
        Task<ListResultDto<EmployeeDto>> GetEmployeesByServiceAsync(Guid serviceId);

        // =============== ÇALIŞMA SAATLERİ ===============

        /// <summary>
        /// Çalışma saatlerini getir
        /// </summary>
        Task<ListResultDto<EmployeeWorkingHoursDto>> GetWorkingHoursAsync(Guid employeeId);

        /// <summary>
        /// Çalışma saati ekle/güncelle
        /// </summary>
        Task SetWorkingHoursAsync(Guid employeeId, List<EmployeeWorkingHoursDto> workingHours);

        /// <summary>
        /// Belirli bir günde çalışan mı?
        /// </summary>
        Task<bool> IsWorkingOnDayAsync(Guid employeeId, DayOfWeek dayOfWeek);

        /// <summary>
        /// Belirli bir tarih/saatte müsait mi?
        /// </summary>
        Task<bool> IsAvailableAsync(Guid employeeId, DateTime dateTime, int durationMinutes);
    }
}