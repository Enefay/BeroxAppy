using BeroxAppy.Enums;
using BeroxAppy.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace BeroxAppy.Employees
{
    public class EmployeeAppService :
        CrudAppService<Employee, EmployeeDto, Guid, GetEmployeesInput, EmployeeDto>,
        IEmployeeAppService
    {
        private readonly IRepository<EmployeeService, Guid> _employeeServiceRepository;
        private readonly IRepository<EmployeeWorkingHours, Guid> _workingHoursRepository;
        private readonly IRepository<Service, Guid> _serviceRepository;
        private readonly IIdentityUserAppService _identityUserAppService;
        private readonly IdentityUserManager _userManager;

        public EmployeeAppService(
            IRepository<Employee, Guid> repository,
            IRepository<EmployeeService, Guid> employeeServiceRepository,
            IRepository<EmployeeWorkingHours, Guid> workingHoursRepository,
            IRepository<Service, Guid> serviceRepository,
            IIdentityUserAppService identityUserAppService,
            IdentityUserManager userManager)
            : base(repository)
        {
            _employeeServiceRepository = employeeServiceRepository;
            _workingHoursRepository = workingHoursRepository;
            _serviceRepository = serviceRepository;
            _identityUserAppService = identityUserAppService;
            _userManager = userManager;
        }

        /// <summary>
        /// Filtreleme ile liste getir (override)
        /// </summary>
        protected override async Task<IQueryable<Employee>> CreateFilteredQueryAsync(GetEmployeesInput input)
        {
            var query = await ReadOnlyRepository.GetQueryableAsync();

            // Temel filtreleme (Ad, soyad, telefon, email)
            if (!string.IsNullOrWhiteSpace(input.Filter))
            {
                query = query.Where(x =>
                    x.FirstName.Contains(input.Filter) ||
                    x.LastName.Contains(input.Filter) ||
                    x.Phone.Contains(input.Filter) ||
                    (x.Email != null && x.Email.Contains(input.Filter)));
            }

            // Çalışan tipi filtresi
            if (input.EmployeeType.HasValue)
            {
                query = query.Where(x => x.EmployeeType == input.EmployeeType);
            }

            // Hizmet verdiği cinsiyet filtresi
            if (input.ServiceGender.HasValue)
            {
                query = query.Where(x => x.ServiceGender == input.ServiceGender);
            }

            // Aktiflik filtresi
            if (input.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == input.IsActive);
            }

            // Online rezervasyon alma durumu
            if (input.CanTakeOnlineReservation.HasValue)
            {
                query = query.Where(x => x.CanTakeOnlineReservation == input.CanTakeOnlineReservation);
            }

            // Kullanıcısı var/yok filtresi
            if (input.HasUser.HasValue)
            {
                if (input.HasUser.Value)
                {
                    query = query.Where(x => x.UserId != null);
                }
                else
                {
                    query = query.Where(x => x.UserId == null);
                }
            }

            // Maaş aralığı
            if (input.MinSalary.HasValue)
            {
                query = query.Where(x => x.FixedSalary >= input.MinSalary);
            }
            if (input.MaxSalary.HasValue)
            {
                query = query.Where(x => x.FixedSalary <= input.MaxSalary);
            }

            // Default sıralama
            if (string.IsNullOrEmpty(input.Sorting))
            {
                query = query.OrderBy(x => x.FirstName).ThenBy(x => x.LastName);
            }

            return query;
        }

        /// <summary>
        /// Liste getir (override) - display alanlarını doldur
        /// </summary>
        public override async Task<PagedResultDto<EmployeeDto>> GetListAsync(GetEmployeesInput input)
        {

            var query = await Repository.GetQueryableAsync();

            if (input.Sorting?.Contains("fullName", StringComparison.OrdinalIgnoreCase) == true)
            {
                query = query.OrderBy(e => e.FirstName).ThenBy(e => e.LastName);
                input.Sorting = null;
            }

            var result = await base.GetListAsync(input);

            // Her bir DTO için display alanlarını doldur
            foreach (var dto in result.Items)
            {
                await EnrichEmployeeDtoAsync(dto);
            }

            return result;
        }

        /// <summary>
        /// Tekil getir (override) - display alanlarını doldur
        /// </summary>
        public override async Task<EmployeeDto> GetAsync(Guid id)
        {
            var dto = await base.GetAsync(id);
            await EnrichEmployeeDtoAsync(dto);
            return dto;
        }

        /// <summary>
        /// Create işleminde özel logic - ABP User oluştur
        /// </summary>
        public override async Task<EmployeeDto> CreateAsync(EmployeeDto input)
        {
            // Email unique kontrolü
            if (!string.IsNullOrWhiteSpace(input.Email))
            {
                await CheckEmailUniquenessAsync(input.Email);
            }

            // Telefon unique kontrolü
            await CheckPhoneUniquenessAsync(input.Phone);

            // Önce Employee'ı oluştur
            var employee = await base.CreateAsync(input);

            // Eğer UserName ve Password verilmişse ABP User oluştur
            if (!string.IsNullOrWhiteSpace(input.UserName) && !string.IsNullOrWhiteSpace(input.Password))
            {
                await CreateUserAsync(employee.Id, input.UserName, input.Password);
            }

            return employee;
        }

        /// <summary>
        /// Update işleminde özel kontroller
        /// </summary>
        public override async Task<EmployeeDto> UpdateAsync(Guid id, EmployeeDto input)
        {
            // Email unique kontrolü (kendisi hariç)
            if (!string.IsNullOrWhiteSpace(input.Email))
            {
                await CheckEmailUniquenessAsync(input.Email, id);
            }

            // Telefon unique kontrolü (kendisi hariç)
            await CheckPhoneUniquenessAsync(input.Phone, id);

            return await base.UpdateAsync(id, input);
        }


        /// <summary>
        /// Custom Update işleminde özel kontroller
        /// </summary>
        public async Task<EmployeeDto> UpdateCustomAsync(Guid id, EmployeeUpdateDto input)
        {
            // Email unique kontrolü (kendisi hariç)
            if (!string.IsNullOrWhiteSpace(input.Email))
            {
                await CheckEmailUniquenessAsync(input.Email, id);
            }

            // Telefon unique kontrolü (kendisi hariç)
            await CheckPhoneUniquenessAsync(input.Phone, id);


            var mappedData = ObjectMapper.Map<EmployeeUpdateDto, EmployeeDto>(input);

            var dbData = await base.GetAsync(id);

            mappedData.UserId = dbData.UserId;

            return await base.UpdateAsync(id, mappedData);
        }

        /// <summary>
        /// Aktif çalışanları getir
        /// </summary>
        public async Task<ListResultDto<EmployeeDto>> GetActiveListAsync()
        {
            var queryable = await Repository.GetQueryableAsync();
            var employees = queryable
                .Where(x => x.IsActive)
                .OrderBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .ToList();

            var dtos = ObjectMapper.Map<List<Employee>, List<EmployeeDto>>(employees);

            foreach (var dto in dtos)
            {
                await EnrichEmployeeDtoAsync(dto);
            }

            return new ListResultDto<EmployeeDto>(dtos);
        }

        /// <summary>
        /// Çalışanı aktif/pasif yap
        /// </summary>
        public async Task SetActiveStatusAsync(Guid id, bool isActive)
        {
            var employee = await Repository.GetAsync(id);
            employee.IsActive = isActive;

            // Eğer çalışan pasif yapılıyorsa kullanıcısını da pasif yap
            if (!isActive && employee.UserId.HasValue)
            {
                await SetUserActiveStatusAsync(id, false);
            }

            await Repository.UpdateAsync(employee);
        }

        /// <summary>
        /// Çalışan maaşını güncelle
        /// </summary>
        public async Task UpdateSalaryAsync(Guid id, decimal newSalary)
        {
            if (newSalary < 0)
            {
                throw new UserFriendlyException("Maaş negatif olamaz.");
            }

            var employee = await Repository.GetAsync(id);
            employee.FixedSalary = newSalary;
            await Repository.UpdateAsync(employee);
        }

        /// <summary>
        /// Komisyon oranlarını güncelle
        /// </summary>
        public async Task UpdateCommissionRatesAsync(Guid id, decimal serviceRate, decimal productRate, decimal packageRate)
        {
            if (serviceRate < 0 || serviceRate > 100)
                throw new UserFriendlyException("Hizmet komisyon oranı 0-100 arasında olmalıdır.");
            if (productRate < 0 || productRate > 100)
                throw new UserFriendlyException("Ürün komisyon oranı 0-100 arasında olmalıdır.");
            if (packageRate < 0 || packageRate > 100)
                throw new UserFriendlyException("Paket komisyon oranı 0-100 arasında olmalıdır.");

            var employee = await Repository.GetAsync(id);
            employee.ServiceCommissionRate = serviceRate;
            employee.ProductCommissionRate = productRate;
            employee.PackageCommissionRate = packageRate;
            await Repository.UpdateAsync(employee);
        }

        /// <summary>
        /// Çalışan için ABP kullanıcısı oluştur
        /// </summary>
        public async Task CreateUserAsync(Guid employeeId, string userName, string password)
        {
            var employee = await Repository.GetAsync(employeeId);

            if (employee.UserId.HasValue)
            {
                throw new UserFriendlyException("Bu çalışanın zaten bir kullanıcısı var.");
            }

            // ABP User oluştur
            var user = new IdentityUserCreateDto
            {
                UserName = userName,
                Password = password,
                Email = employee.Email,
                Name = employee.FirstName,
                Surname = employee.LastName,
                PhoneNumber = employee.Phone,
                IsActive = employee.IsActive,
                LockoutEnabled = false
            };

            var createdUser = await _identityUserAppService.CreateAsync(user);

            // Employee'a UserId'yi ata
            employee.UserId = createdUser.Id;
            await Repository.UpdateAsync(employee);
        }

        /// <summary>
        /// Çalışan kullanıcısını aktif/pasif yap
        /// </summary>
        public async Task SetUserActiveStatusAsync(Guid employeeId, bool isActive)
        {
            var employee = await Repository.GetAsync(employeeId);

            if (!employee.UserId.HasValue)
            {
                throw new UserFriendlyException("Bu çalışanın kullanıcısı bulunmamaktadır.");
            }

            var user = await _userManager.FindByIdAsync(employee.UserId.Value.ToString());
            if (user != null)
            {
                //user.IsActive = isActive;
                await _userManager.UpdateAsync(user);
            }
        }

        /// <summary>
        /// Kullanıcı şifresini sıfırla
        /// </summary>
        public async Task ResetUserPasswordAsync(Guid employeeId, string newPassword)
        {
            var employee = await Repository.GetAsync(employeeId);

            if (!employee.UserId.HasValue)
            {
                throw new UserFriendlyException("Bu çalışanın kullanıcısı bulunmamaktadır.");
            }

            var user = await _userManager.FindByIdAsync(employee.UserId.Value.ToString());
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, token, newPassword);
            }
        }

        // =============== HİZMET YÖNETİMİ ===============

        /// <summary>
        /// Çalışana hizmet ata
        /// </summary>
        public async Task AssignServiceAsync(Guid employeeId, Guid serviceId)
        {
            // Zaten atanmış mı kontrol et
            var existing = await _employeeServiceRepository.FindAsync(x =>
                x.EmployeeId == employeeId && x.ServiceId == serviceId);

            if (existing != null)
            {
                throw new UserFriendlyException("Bu hizmet zaten çalışana atanmış.");
            }

            var employeeService = new EmployeeService
            {
                EmployeeId = employeeId,
                ServiceId = serviceId
            };

            await _employeeServiceRepository.InsertAsync(employeeService);
        }

        /// <summary>
        /// Çalışandan hizmet kaldır
        /// </summary>
        public async Task UnassignServiceAsync(Guid employeeId, Guid serviceId)
        {
            var employeeService = await _employeeServiceRepository.FindAsync(x =>
                x.EmployeeId == employeeId && x.ServiceId == serviceId);

            if (employeeService == null)
            {
                throw new UserFriendlyException("Bu hizmet çalışana atanmamış.");
            }

            await _employeeServiceRepository.DeleteAsync(employeeService);
        }

        /// <summary>
        /// Çalışanın hizmetlerini getir
        /// </summary>
        public async Task<ListResultDto<EmployeeServiceAssignmentDto>> GetEmployeeServicesAsync(Guid employeeId)
        {
            var queryable = await _employeeServiceRepository.GetQueryableAsync();
            var serviceQueryable = await _serviceRepository.GetQueryableAsync();

            var assignments = from es in queryable
                              join s in serviceQueryable on es.ServiceId equals s.Id
                              where es.EmployeeId == employeeId
                              select new EmployeeServiceAssignmentDto
                              {
                                  Id = es.Id,
                                  EmployeeId = es.EmployeeId,
                                  ServiceId = es.ServiceId,
                                  ServiceTitle = s.Title,
                                  ServiceCategoryName = s.Category.Name,
                                  ServicePrice = s.Price,
                                  ServiceDuration = s.DurationMinutes
                              };

            var result = assignments.ToList();
            return new ListResultDto<EmployeeServiceAssignmentDto>(result);
        }

        /// <summary>
        /// Hizmeti verebilen çalışanları getir
        /// </summary>
        public async Task<ListResultDto<EmployeeDto>> GetEmployeesByServiceAsync(Guid serviceId)
        {
            var queryable = await _employeeServiceRepository.GetQueryableAsync();
            var employeeQueryable = await Repository.GetQueryableAsync();

            var employees = from es in queryable
                            join e in employeeQueryable on es.EmployeeId equals e.Id
                            where es.ServiceId == serviceId && e.IsActive
                            select e;

            var result = employees.ToList();
            var dtos = ObjectMapper.Map<List<Employee>, List<EmployeeDto>>(result);

            foreach (var dto in dtos)
            {
                await EnrichEmployeeDtoAsync(dto);
            }

            return new ListResultDto<EmployeeDto>(dtos);
        }


        /// <summary>
        /// Çalışan Arama
        /// </summary>
        public async Task<List<EmployeeDto>> GetEmployeesByServiceandQueryAsync(Guid serviceId,string? query, int maxResultCount = 5)
        {
            var queryable = await _employeeServiceRepository.GetQueryableAsync();
            var employeeQueryable = await Repository.GetQueryableAsync();

            var employees = from es in queryable
                            join e in employeeQueryable on es.EmployeeId equals e.Id
                            where es.ServiceId == serviceId && e.IsActive
                            select e;

            if (!string.IsNullOrWhiteSpace(query))
            {
                employees = employees.Where(e =>
                    (e.FirstName + " " + e.LastName).Contains(query));
            }


            var result = await AsyncExecuter.ToListAsync(
                    employees
                        .OrderBy(e => e.FirstName)
                        .ThenBy(e => e.LastName)
                        .Take(maxResultCount)
                );
            
            var dtos = ObjectMapper.Map<List<Employee>, List<EmployeeDto>>(result);

            foreach (var dto in dtos)
            {
                await EnrichEmployeeDtoAsync(dto);
            }

            return dtos;
        }


        // =============== ÇALIŞMA SAATLERİ ===============

        /// <summary>
        /// Çalışma saatlerini getir
        /// </summary>
        public async Task<ListResultDto<EmployeeWorkingHoursDto>> GetWorkingHoursAsync(Guid employeeId)
        {
            var queryable = await _workingHoursRepository.GetQueryableAsync();
            var workingHours = queryable
                .Where(x => x.EmployeeId == employeeId)
                .OrderBy(x => x.DayOfWeek)
                .ToList();

            var dtos = ObjectMapper.Map<List<EmployeeWorkingHours>, List<EmployeeWorkingHoursDto>>(workingHours);

            foreach (var dto in dtos)
            {
                EnrichWorkingHoursDto(dto);
            }

            return new ListResultDto<EmployeeWorkingHoursDto>(dtos);
        }

        /// <summary>
        /// Çalışma saati ekle/güncelle
        /// </summary>
        public async Task SetWorkingHoursAsync(Guid employeeId, List<EmployeeWorkingHoursDto> workingHours)
        {
            // Mevcut çalışma saatlerini sil
            var existing = await _workingHoursRepository.GetListAsync(x => x.EmployeeId == employeeId);
            foreach (var item in existing)
            {
                await _workingHoursRepository.DeleteAsync(item);
            }

            // Yeni çalışma saatlerini ekle
            foreach (var dto in workingHours.Where(x => x.IsActive))
            {
                var workingHour = new EmployeeWorkingHours
                {
                    EmployeeId = employeeId,
                    DayOfWeek = dto.DayOfWeek,
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime,
                    BreakStartTime = dto.BreakStartTime,
                    BreakEndTime = dto.BreakEndTime,
                    IsActive = dto.IsActive
                };

                await _workingHoursRepository.InsertAsync(workingHour);
            }
        }

        /// <summary>
        /// Belirli bir günde çalışan mı?
        /// </summary>
        public async Task<bool> IsWorkingOnDayAsync(Guid employeeId, DayOfWeek dayOfWeek)
        {
            var queryable = await _workingHoursRepository.GetQueryableAsync();
            return queryable.Any(x => x.EmployeeId == employeeId &&
                                     x.DayOfWeek == dayOfWeek &&
                                     x.IsActive);
        }

        /// <summary>
        /// Belirli bir tarih/saatte müsait mi?
        /// </summary>
        public async Task<bool> IsAvailableAsync(Guid employeeId, DateTime dateTime, int durationMinutes)
        {
            // Önce o gün çalışıyor mu kontrol et
            var isWorking = await IsWorkingOnDayAsync(employeeId, dateTime.DayOfWeek);
            if (!isWorking) return false;

            var queryable = await _workingHoursRepository.GetQueryableAsync();
            var workingHour = queryable.FirstOrDefault(x =>
                x.EmployeeId == employeeId &&
                x.DayOfWeek == dateTime.DayOfWeek &&
                x.IsActive);

            if (workingHour == null) return false;

            var requestTime = dateTime.TimeOfDay;
            var endTime = requestTime.Add(TimeSpan.FromMinutes(durationMinutes));

            // Çalışma saatleri içinde mi?
            if (requestTime < workingHour.StartTime || endTime > workingHour.EndTime)
                return false;

            // Mola saatinde mi?
            if (workingHour.BreakStartTime.HasValue && workingHour.BreakEndTime.HasValue)
            {
                if (!(endTime <= workingHour.BreakStartTime || requestTime >= workingHour.BreakEndTime))
                    return false; // Mola saatine denk geliyor
            }

            return true;
        }

        /// <summary>
        /// Silme işleminde özel kontrol
        /// </summary>
        public override async Task DeleteAsync(Guid id)
        {
            var employee = await Repository.GetAsync(id);

            // Rezervasyonları var mı kontrol et
            if (employee.ReservationDetails?.Any() == true)
            {
                throw new UserFriendlyException("Bu çalışana ait rezervasyonlar bulunmaktadır. Çalışanı silmek yerine pasif yapabilirsiniz.");
            }

            // Kullanıcısı varsa onu da sil
            if (employee.UserId.HasValue)
            {
                await _identityUserAppService.DeleteAsync(employee.UserId.Value);
            }

            await base.DeleteAsync(id);
        }


    



        /// <summary>
        /// EmployeeDto'yu zenginleştir
        /// </summary>
        private async Task EnrichEmployeeDtoAsync(EmployeeDto dto)
        {
            // Tam ad
            dto.FullName = $"{dto.FirstName} {dto.LastName}";

            // Çalışan tipi display
            dto.EmployeeTypeDisplay = dto.EmployeeType switch
            {
                EmployeeType.Staff => "Personel",
                EmployeeType.Secretary => "Sekreter",
                EmployeeType.Manager => "Müdür",
                EmployeeType.Device => "Cihaz",
                _ => "Bilinmiyor"
            };

            // Hizmet verdiği cinsiyet display
            dto.ServiceGenderDisplay = dto.ServiceGender switch
            {
                Gender.Male => "Erkek",
                Gender.Female => "Kadın",
                Gender.Unisex => "Unisex",
                _ => "Bilinmiyor"
            };

            // Kullanıcı durumu
            dto.HasUser = dto.UserId.HasValue;
            if (dto.HasUser)
            {
                try
                {
                    var user = await _userManager.FindByIdAsync(dto.UserId.Value.ToString());
                    dto.UserStatus = user?.IsActive == true ? "Aktif" : "Pasif";
                }
                catch
                {
                    dto.UserStatus = "Hata";
                }
            }
            else
            {
                dto.UserStatus = "Kullanıcı Yok";
            }
        }

        /// <summary>
        /// Çalışma saatleri DTO'sunu zenginleştir
        /// </summary>
        private void EnrichWorkingHoursDto(EmployeeWorkingHoursDto dto)
        {
            // Gün adı
            dto.DayOfWeekDisplay = dto.DayOfWeek switch
            {
                DayOfWeek.Monday => "Pazartesi",
                DayOfWeek.Tuesday => "Salı",
                DayOfWeek.Wednesday => "Çarşamba",
                DayOfWeek.Thursday => "Perşembe",
                DayOfWeek.Friday => "Cuma",
                DayOfWeek.Saturday => "Cumartesi",
                DayOfWeek.Sunday => "Pazar",
                _ => "Bilinmiyor"
            };

            // Çalışma saatleri display
            dto.WorkingHoursDisplay = $"{dto.StartTime:hh\\:mm} - {dto.EndTime:hh\\:mm}";

            // Mola display
            if (dto.BreakStartTime.HasValue && dto.BreakEndTime.HasValue)
            {
                dto.BreakDisplay = $"{dto.BreakStartTime:hh\\:mm} - {dto.BreakEndTime:hh\\:mm}";
            }
            else
            {
                dto.BreakDisplay = "Mola Yok";
            }
        }

        /// <summary>
        /// Email unique kontrolü
        /// </summary>
        private async Task CheckEmailUniquenessAsync(string email, Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return;

            var queryable = await Repository.GetQueryableAsync();
            var exists = queryable.Any(x => x.Email == email.Trim() &&
                                           (excludeId == null || x.Id != excludeId));

            if (exists)
            {
                throw new UserFriendlyException("Bu email adresi zaten kayıtlı.");
            }
        }

        /// <summary>
        /// Telefon unique kontrolü
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