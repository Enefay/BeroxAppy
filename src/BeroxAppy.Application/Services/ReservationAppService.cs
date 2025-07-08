using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BeroxAppy.Customers;
using BeroxAppy.Employees;
using BeroxAppy.Enums;
using BeroxAppy.Services;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace BeroxAppy.Reservations
{
    public class ReservationAppService : ApplicationService, IReservationAppService
    {
        private readonly IRepository<Reservation, Guid> _reservationRepository;
        private readonly IRepository<ReservationDetail, Guid> _reservationDetailRepository;
        private readonly IRepository<Customer, Guid> _customerRepository;
        private readonly IRepository<Employee, Guid> _employeeRepository;
        private readonly IRepository<Service, Guid> _serviceRepository;
        private readonly IRepository<EmployeeWorkingHours, Guid> _workingHoursRepository;

        public ReservationAppService(
            IRepository<Reservation, Guid> reservationRepository,
            IRepository<ReservationDetail, Guid> reservationDetailRepository,
            IRepository<Customer, Guid> customerRepository,
            IRepository<Employee, Guid> employeeRepository,
            IRepository<Service, Guid> serviceRepository,
            IRepository<EmployeeWorkingHours, Guid> workingHoursRepository)
        {
            _reservationRepository = reservationRepository;
            _reservationDetailRepository = reservationDetailRepository;
            _customerRepository = customerRepository;
            _employeeRepository = employeeRepository;
            _serviceRepository = serviceRepository;
            _workingHoursRepository = workingHoursRepository;
        }

        // =============== TEMEL CRUD ===============

        /// <summary>
        /// Rezervasyon listesi getir
        /// </summary>
        public async Task<PagedResultDto<ReservationDto>> GetListAsync(GetReservationsInput input)
        {
            var queryable = await _reservationRepository.GetQueryableAsync();

            // Filtreleme
            if (!string.IsNullOrWhiteSpace(input.Filter))
            {
                queryable = queryable.Where(x =>
                    x.Customer.FullName.Contains(input.Filter) ||
                    x.Customer.Phone.Contains(input.Filter));
            }

            if (input.StartDate.HasValue)
            {
                queryable = queryable.Where(x => x.ReservationDate >= input.StartDate.Value.Date);
            }

            if (input.EndDate.HasValue)
            {
                queryable = queryable.Where(x => x.ReservationDate <= input.EndDate.Value.Date.AddDays(1));
            }

            if (input.CustomerId.HasValue)
            {
                queryable = queryable.Where(x => x.CustomerId == input.CustomerId);
            }

            if (input.EmployeeId.HasValue)
            {
                queryable = queryable.Where(x => x.ReservationDetails.Any(rd => rd.EmployeeId == input.EmployeeId));
            }

            if (input.ServiceId.HasValue)
            {
                queryable = queryable.Where(x => x.ReservationDetails.Any(rd => rd.ServiceId == input.ServiceId));
            }

            if (input.Status.HasValue)
            {
                queryable = queryable.Where(x => x.Status == input.Status);
            }

            if (input.PaymentStatus.HasValue)
            {
                queryable = queryable.Where(x => x.PaymentStatus == input.PaymentStatus);
            }

            if (input.IsWalkIn.HasValue)
            {
                queryable = queryable.Where(x => x.IsWalkIn == input.IsWalkIn);
            }

            if (input.IsToday.HasValue && input.IsToday.Value)
            {
                var today = DateTime.Now.Date;
                queryable = queryable.Where(x => x.ReservationDate.Date == today);
            }

            if (input.IsPast.HasValue)
            {
                var now = DateTime.Now;
                if (input.IsPast.Value)
                {
                    queryable = queryable.Where(x => x.ReservationDate.Add(x.EndTime) < now);
                }
                else
                {
                    queryable = queryable.Where(x => x.ReservationDate.Add(x.EndTime) >= now);
                }
            }

            if (input.MinAmount.HasValue)
            {
                queryable = queryable.Where(x => x.FinalPrice >= input.MinAmount);
            }

            if (input.MaxAmount.HasValue)
            {
                queryable = queryable.Where(x => x.FinalPrice <= input.MaxAmount);
            }

            // Sıralama
            if (string.IsNullOrEmpty(input.Sorting))
            {
                queryable = queryable.OrderByDescending(x => x.ReservationDate).ThenBy(x => x.StartTime);
            }

            // Sayfalama
            var totalCount = queryable.Count();
            var reservations = queryable
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .ToList();

            // DTO'ya çevir ve zenginleştir
            var dtos = new List<ReservationDto>();
            foreach (var reservation in reservations)
            {
                var dto = await MapToReservationDtoAsync(reservation);
                dtos.Add(dto);
            }

            return new PagedResultDto<ReservationDto>(totalCount, dtos);
        }

        /// <summary>
        /// Rezervasyon detayı getir
        /// </summary>
        public async Task<ReservationDto> GetAsync(Guid id)
        {
            var reservation = await _reservationRepository.GetAsync(id);
            return await MapToReservationDtoAsync(reservation);
        }

        /// <summary>
        /// Yeni rezervasyon oluştur
        /// </summary>
        public async Task<ReservationDto> CreateAsync(CreateReservationDto input)
        {
            // Validation
            await ValidateCreateReservationAsync(input);

            // Rezervasyon entity'si oluştur
            var reservation = new Reservation
            {
                CustomerId = input.CustomerId,
                Note = input.Note,
                ReservationDate = input.ReservationDate.Date,
                DiscountAmount = input.DiscountAmount,
                ExtraAmount = input.ExtraAmount,
                IsWalkIn = input.IsWalkIn,
                Status = ReservationStatus.Pending,
                PaymentStatus = PaymentStatus.Pending,
                ReminderSent = false
            };

            // Hizmet detaylarını ekle ve süreleri hesapla
            var details = new List<ReservationDetail>();
            var currentTime = input.ReservationDetails.Min(x => x.StartTime);
            reservation.StartTime = currentTime;

            decimal totalServicePrice = 0;

            foreach (var detailInput in input.ReservationDetails.OrderBy(x => x.StartTime))
            {
                var service = await _serviceRepository.GetAsync(detailInput.ServiceId);
                var employee = await _employeeRepository.GetAsync(detailInput.EmployeeId);

                // Müsaitlik kontrol et
                var isAvailable = await CheckAvailabilityAsync(
                    detailInput.EmployeeId,
                    input.ReservationDate.Add(detailInput.StartTime),
                    service.DurationMinutes);

                if (!isAvailable)
                {
                    throw new UserFriendlyException($"{employee.FirstName} {employee.LastName} çalışanı {detailInput.StartTime:hh\\:mm} saatinde müsait değil.");
                }

                var endTime = detailInput.StartTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));
                var servicePrice = detailInput.CustomPrice ?? service.Price;

                var detail = new ReservationDetail
                {
                    ServiceId = detailInput.ServiceId,
                    EmployeeId = detailInput.EmployeeId,
                    ServicePrice = servicePrice,
                    StartTime = detailInput.StartTime,
                    EndTime = endTime,
                    CommissionRate = employee.ServiceCommissionRate,
                    CommissionAmount = servicePrice * employee.ServiceCommissionRate / 100,
                    Note = detailInput.Note
                };

                details.Add(detail);
                totalServicePrice += servicePrice;

                // En geç bitiş zamanını belirle
                if (endTime > reservation.EndTime)
                {
                    reservation.EndTime = endTime;
                }
            }

            // Fiyat hesaplamaları
            reservation.TotalServicePrice = totalServicePrice;
            reservation.FinalPrice = totalServicePrice;

            if (input.DiscountAmount.HasValue)
            {
                reservation.FinalPrice -= input.DiscountAmount.Value;
            }

            if (input.ExtraAmount.HasValue)
            {
                reservation.FinalPrice += input.ExtraAmount.Value;
            }

            // Müşteri indirimi uygula
            var customer = await _customerRepository.GetAsync(input.CustomerId);
            if (customer.DiscountRate > 0)
            {
                var customerDiscount = reservation.FinalPrice * customer.DiscountRate / 100;
                reservation.FinalPrice -= customerDiscount;

                if (reservation.DiscountAmount.HasValue)
                {
                    reservation.DiscountAmount += customerDiscount;
                }
                else
                {
                    reservation.DiscountAmount = customerDiscount;
                }
            }

            // Negatif fiyat kontrolü
            if (reservation.FinalPrice < 0)
            {
                reservation.FinalPrice = 0;
            }

            // Rezervasyonu kaydet
            await _reservationRepository.InsertAsync(reservation);

            // Detayları kaydet
            foreach (var detail in details)
            {
                detail.ReservationId = reservation.Id;
                await _reservationDetailRepository.InsertAsync(detail);
            }

            return await GetAsync(reservation.Id);
        }

        /// <summary>
        /// Rezervasyon güncelle
        /// </summary>
        public async Task<ReservationDto> UpdateAsync(Guid id, UpdateReservationDto input)
        {
            var reservation = await _reservationRepository.GetAsync(id);

            reservation.CustomerId = input.CustomerId;
            reservation.Note = input.Note;
            reservation.ReservationDate = input.ReservationDate.Date;
            reservation.DiscountAmount = input.DiscountAmount;
            reservation.ExtraAmount = input.ExtraAmount;
            reservation.Status = input.Status;

            // Fiyatları yeniden hesapla
            await RecalculatePricesAsync(id);

            await _reservationRepository.UpdateAsync(reservation);

            return await GetAsync(id);
        }

        /// <summary>
        /// Rezervasyon sil
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            // Önce detayları sil
            var details = await _reservationDetailRepository.GetListAsync(x => x.ReservationId == id);
            foreach (var detail in details)
            {
                await _reservationDetailRepository.DeleteAsync(detail);
            }

            // Rezervasyonu sil
            await _reservationRepository.DeleteAsync(id);
        }

        // =============== DURUM YÖNETİMİ ===============

        /// <summary>
        /// Rezervasyon durumunu güncelle
        /// </summary>
        public async Task UpdateStatusAsync(Guid id, ReservationStatus status)
        {
            var reservation = await _reservationRepository.GetAsync(id);
            reservation.Status = status;
            await _reservationRepository.UpdateAsync(reservation);
        }

        /// <summary>
        /// Ödeme durumunu güncelle
        /// </summary>
        public async Task UpdatePaymentStatusAsync(Guid id, PaymentStatus paymentStatus)
        {
            var reservation = await _reservationRepository.GetAsync(id);
            reservation.PaymentStatus = paymentStatus;
            await _reservationRepository.UpdateAsync(reservation);
        }

        /// <summary>
        /// Müşteri geldi işaretle
        /// </summary>
        public async Task MarkAsArrivedAsync(Guid id)
        {
            await UpdateStatusAsync(id, ReservationStatus.Arrived);
        }

        /// <summary>
        /// Müşteri gelmedi işaretle
        /// </summary>
        public async Task MarkAsNoShowAsync(Guid id)
        {
            await UpdateStatusAsync(id, ReservationStatus.NoShow);
        }

        // =============== HELPER METHODS ===============

        /// <summary>
        /// Reservation'ı DTO'ya çevir ve zenginleştir
        /// </summary>
        private async Task<ReservationDto> MapToReservationDtoAsync(Reservation reservation)
        {
            var dto = ObjectMapper.Map<Reservation, ReservationDto>(reservation);

            // Müşteri bilgileri
            var customer = await _customerRepository.FindAsync(reservation.CustomerId);
            if (customer != null)
            {
                dto.CustomerName = customer.FullName;
                dto.CustomerPhone = customer.Phone;
            }

            // Display alanları
            dto.StatusDisplay = GetReservationStatusDisplay(reservation.Status);
            dto.PaymentStatusDisplay = GetPaymentStatusDisplay(reservation.PaymentStatus);
            dto.ReservationTimeDisplay = $"{reservation.StartTime:hh\\:mm} - {reservation.EndTime:hh\\:mm}";
            dto.ReservationTypeDisplay = reservation.IsWalkIn ? "Adisyon" : "Rezervasyon";

            // Süre hesaplamaları
            var duration = reservation.EndTime - reservation.StartTime;
            dto.TotalDurationMinutes = (int)duration.TotalMinutes;
            dto.DurationDisplay = FormatDuration(dto.TotalDurationMinutes);

            // Tarih kontrolleri
            var now = DateTime.Now;
            dto.IsToday = reservation.ReservationDate.Date == now.Date;
            dto.IsPast = reservation.ReservationDate.Add(reservation.EndTime) < now;

            // Rezervasyon detayları
            var details = await _reservationDetailRepository.GetListAsync(x => x.ReservationId == reservation.Id);
            dto.ReservationDetails = new List<ReservationDetailDto>();

            foreach (var detail in details)
            {
                var detailDto = await MapToReservationDetailDtoAsync(detail);
                dto.ReservationDetails.Add(detailDto);
            }

            return dto;
        }

        /// <summary>
        /// ReservationDetail'ı DTO'ya çevir
        /// </summary>
        private async Task<ReservationDetailDto> MapToReservationDetailDtoAsync(ReservationDetail detail)
        {
            var dto = ObjectMapper.Map<ReservationDetail, ReservationDetailDto>(detail);

            // Service bilgileri
            var service = await _serviceRepository.FindAsync(detail.ServiceId);
            if (service != null)
            {
                dto.ServiceTitle = service.Title;
                dto.DurationMinutes = service.DurationMinutes;
                dto.DurationDisplay = FormatDuration(service.DurationMinutes);

                // Kategori bilgisi
                if (service.CategoryId.HasValue)
                {
                    var categoryQueryable = await _serviceRepository.GetQueryableAsync();
                    // ServiceCategory'yi manuel sorgulamalıyız
                    dto.ServiceCategoryName = "Kategori"; // Basitleştirme
                }
            }

            // Employee bilgileri
            var employee = await _employeeRepository.FindAsync(detail.EmployeeId);
            if (employee != null)
            {
                dto.EmployeeName = $"{employee.FirstName} {employee.LastName}";
                dto.EmployeeColor = employee.CalendarColor;
            }

            // Display alanları
            dto.TimeDisplay = $"{detail.StartTime:hh\\:mm} - {detail.EndTime:hh\\:mm}";

            return dto;
        }

        /// <summary>
        /// Create validation
        /// </summary>
        private async Task ValidateCreateReservationAsync(CreateReservationDto input)
        {
            // Müşteri var mı?
            var customerExists = await _customerRepository.AnyAsync(x => x.Id == input.CustomerId);
            if (!customerExists)
            {
                throw new UserFriendlyException("Belirtilen müşteri bulunamadı.");
            }

            // En az bir hizmet var mı?
            if (!input.ReservationDetails.Any())
            {
                throw new UserFriendlyException("En az bir hizmet seçilmelidir.");
            }

            // Tarih geçmiş mi?
            if (input.ReservationDate.Date < DateTime.Now.Date)
            {
                throw new UserFriendlyException("Geçmiş tarihe rezervasyon oluşturulamaz.");
            }

            // Hizmet ve çalışan kontrolleri
            foreach (var detail in input.ReservationDetails)
            {
                var serviceExists = await _serviceRepository.AnyAsync(x => x.Id == detail.ServiceId && x.IsActive);
                if (!serviceExists)
                {
                    throw new UserFriendlyException("Belirtilen hizmetlerden biri bulunamadı veya aktif değil.");
                }

                var employeeExists = await _employeeRepository.AnyAsync(x => x.Id == detail.EmployeeId && x.IsActive);
                if (!employeeExists)
                {
                    throw new UserFriendlyException("Belirtilen çalışanlardan biri bulunamadı veya aktif değil.");
                }
            }
        }

        /// <summary>
        /// Display formatları
        /// </summary>
        private string GetReservationStatusDisplay(ReservationStatus status)
        {
            return status switch
            {
                ReservationStatus.Pending => "Beklemede",
                ReservationStatus.NoShow => "Gelmedi",
                ReservationStatus.Arrived => "Geldi",
                _ => "Bilinmiyor"
            };
        }

        private string GetPaymentStatusDisplay(PaymentStatus status)
        {
            return status switch
            {
                PaymentStatus.Pending => "Beklemede",
                PaymentStatus.Partial => "Kısmi Ödendi",
                PaymentStatus.Paid => "Ödendi",
                PaymentStatus.Refunded => "İade Edildi",
                _ => "Bilinmiyor"
            };
        }

        private string FormatDuration(int minutes)
        {
            if (minutes < 60)
                return $"{minutes} dakika";

            var hours = minutes / 60;
            var remainingMinutes = minutes % 60;

            if (remainingMinutes == 0)
                return $"{hours} saat";

            return $"{hours} saat {remainingMinutes} dakika";
        }

        // =============== MÜSAİTLİK KONTROLÜ ===============

        /// <summary>
        /// Belirli tarih/saat için müsaitlik kontrol et
        /// </summary>
        public async Task<bool> CheckAvailabilityAsync(Guid employeeId, DateTime dateTime, int durationMinutes)
        {
            // Çalışma saatleri kontrolü
            var workingHours = await _workingHoursRepository.FindAsync(x =>
                x.EmployeeId == employeeId &&
                x.DayOfWeek == dateTime.DayOfWeek &&
                x.IsActive);

            if (workingHours == null)
            {
                return false; // O gün çalışmıyor
            }

            var requestTime = dateTime.TimeOfDay;
            var endTime = requestTime.Add(TimeSpan.FromMinutes(durationMinutes));

            // Çalışma saatleri içinde mi?
            if (requestTime < workingHours.StartTime || endTime > workingHours.EndTime)
            {
                return false;
            }

            // Mola saatinde mi?
            if (workingHours.BreakStartTime.HasValue && workingHours.BreakEndTime.HasValue)
            {
                if (!(endTime <= workingHours.BreakStartTime || requestTime >= workingHours.BreakEndTime))
                {
                    return false; // Mola saatine denk geliyor
                }
            }

            // Başka rezervasyon var mı?
            var existingReservations = await _reservationDetailRepository.GetListAsync(x =>
                x.EmployeeId == employeeId);

            var conflictingReservations = existingReservations.Where(x =>
            {
                // Aynı gün mü?
                var reservationQueryable = await _reservationRepository.GetQueryableAsync();
                if (!reservationQueryable.Any(r => r.Id == x.ReservationId && r.ReservationDate.Date == dateTime.Date))
                    return false;

                // Saatler çakışıyor mu?
                return !(endTime <= x.StartTime || requestTime >= x.EndTime);
            });

            return !conflictingReservations.Any();
        }

        /// <summary>
        /// Çalışan için müsait saatleri getir
        /// </summary>
        public async Task<AvailabilityCheckDto> GetAvailableSlotsAsync(Guid employeeId, Guid serviceId, DateTime date)
        {
            var service = await _serviceRepository.GetAsync(serviceId);
            var result = new AvailabilityCheckDto
            {
                Date = date,
                EmployeeId = employeeId,
                ServiceId = serviceId
            };

            // Çalışma saatleri
            var workingHours = await _workingHoursRepository.FindAsync(x =>
                x.EmployeeId == employeeId &&
                x.DayOfWeek == date.DayOfWeek &&
                x.IsActive);

            if (workingHours == null)
            {
                return result; // Boş liste döner
            }

            // 15 dakikalık slotlar oluştur
            var currentTime = workingHours.StartTime;
            var slots = new List<TimeSlotDto>();

            while (currentTime.Add(TimeSpan.FromMinutes(service.DurationMinutes)) <= workingHours.EndTime)
            {
                var endTime = currentTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));
                var dateTime = date.Add(currentTime);

                var isAvailable = await CheckAvailabilityAsync(employeeId, dateTime, service.DurationMinutes);

                slots.Add(new TimeSlotDto
                {
                    StartTime = currentTime,
                    EndTime = endTime,
                    Display = $"{currentTime:hh\\:mm} - {endTime:hh\\:mm}",
                    IsAvailable = isAvailable
                });

                currentTime = currentTime.Add(TimeSpan.FromMinutes(15)); // 15 dakika aralık
            }

            result.AvailableSlots = slots;
            return result;
        }

        /// <summary>
        /// Hizmet için müsait çalışanları getir
        /// </summary>
        public async Task<List<Guid>> GetAvailableEmployeesAsync(Guid serviceId, DateTime dateTime, int durationMinutes)
        {
            // Hizmeti verebilen çalışanları bul
            var employeeQueryable = await _employeeRepository.GetQueryableAsync();
            var serviceEmployees = employeeQueryable
                .Where(x => x.IsActive && x.EmployeeServices.Any(es => es.ServiceId == serviceId))
                .Select(x => x.Id)
                .ToList();

            var availableEmployees = new List<Guid>();

            foreach (var employeeId in serviceEmployees)
            {
                var isAvailable = await CheckAvailabilityAsync(employeeId, dateTime, durationMinutes);
                if (isAvailable)
                {
                    availableEmployees.Add(employeeId);
                }
            }

            return availableEmployees;
        }

        /// <summary>
        /// Rezervasyon fiyatını yeniden hesapla
        /// </summary>
        public async Task RecalculatePricesAsync(Guid reservationId)
        {
            var reservation = await _reservationRepository.GetAsync(reservationId);
            var details = await _reservationDetailRepository.GetListAsync(x => x.ReservationId == reservationId);

            // Toplam hizmet fiyatı
            var totalServicePrice = details.Sum(x => x.ServicePrice);
            reservation.TotalServicePrice = totalServicePrice;

            // Final fiyat hesaplama
            reservation.FinalPrice = totalServicePrice;

            if (reservation.DiscountAmount.HasValue)
            {
                reservation.FinalPrice -= reservation.DiscountAmount.Value;
            }

            if (reservation.ExtraAmount.HasValue)
            {
                reservation.FinalPrice += reservation.ExtraAmount.Value;
            }

            // Negatif fiyat kontrolü
            if (reservation.FinalPrice < 0)
            {
                reservation.FinalPrice = 0;
            }

            await _reservationRepository.UpdateAsync(reservation);
        }

        public Task<List<CalendarEventDto>> GetCalendarEventsAsync(DateTime startDate, DateTime endDate, Guid? employeeId = null)
        {
            throw new NotImplementedException();
        }

        public Task<List<ReservationDto>> GetTodayReservationsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<List<ReservationDto>> GetUpcomingReservationsAsync(int hoursAhead = 2)
        {
            throw new NotImplementedException();
        }

        public Task<List<ReservationDto>> GetCustomerReservationsAsync(Guid customerId, int maxCount = 10)
        {
            throw new NotImplementedException();
        }

        public Task AddServiceToReservationAsync(Guid reservationId, CreateReservationDetailDto serviceDetail)
        {
            throw new NotImplementedException();
        }

        public Task RemoveServiceFromReservationAsync(Guid reservationId, Guid reservationDetailId)
        {
            throw new NotImplementedException();
        }

        public Task UpdateReservationDetailAsync(Guid reservationDetailId, UpdateReservationDetailDto input)
        {
            throw new NotImplementedException();
        }

        public Task ApplyDiscountAsync(Guid reservationId, decimal discountAmount)
        {
            throw new NotImplementedException();
        }

        public Task ApplyExtraChargeAsync(Guid reservationId, decimal extraAmount, string reason)
        {
            throw new NotImplementedException();
        }

        public Task SendReminderAsync(Guid reservationId)
        {
            throw new NotImplementedException();
        }

        public Task SendBulkRemindersAsync(DateTime targetDate)
        {
            throw new NotImplementedException();
        }

        public Task<DailyReservationReportDto> GetDailyReportAsync(DateTime date)
        {
            throw new NotImplementedException();
        }

        public Task<EmployeePerformanceReportDto> GetEmployeePerformanceAsync(Guid employeeId, DateTime startDate, DateTime endDate)
        {
            throw new NotImplementedException();
        }
    }
}