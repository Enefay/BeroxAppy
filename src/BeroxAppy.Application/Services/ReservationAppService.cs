using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BeroxAppy.Customers;
using BeroxAppy.Employees;
using BeroxAppy.Enums;
using BeroxAppy.Finances;
using BeroxAppy.Services;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

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
        private readonly IPaymentAppService _paymentAppService;

        public ReservationAppService(
            IRepository<Reservation, Guid> reservationRepository,
            IRepository<ReservationDetail, Guid> reservationDetailRepository,
            IRepository<Customer, Guid> customerRepository,
            IRepository<Employee, Guid> employeeRepository,
            IRepository<Service, Guid> serviceRepository,
            IRepository<EmployeeWorkingHours, Guid> workingHoursRepository,
            IPaymentAppService paymentAppService)
        {
            _reservationRepository = reservationRepository;
            _reservationDetailRepository = reservationDetailRepository;
            _customerRepository = customerRepository;
            _employeeRepository = employeeRepository;
            _serviceRepository = serviceRepository;
            _workingHoursRepository = workingHoursRepository;
            _paymentAppService = paymentAppService;
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

            var mappedData = ObjectMapper.Map<Reservation, ReservationDto>(reservation);
            return mappedData;
            
        }

        /// <summary>
        /// Rezervasyon güncelle
        /// </summary>
        /// <summary>
        /// Rezervasyon güncelle
        /// </summary>
        public async Task<ReservationDto> UpdateAsync(Guid id, UpdateReservationDto input)
        {
            // Reservation entity'sini al (DTO değil)
            var reservation = await _reservationRepository.GetAsync(id);

            // Ana rezervasyon bilgilerini güncelle
            reservation.CustomerId = input.CustomerId;
            reservation.Note = input.Note;
            reservation.ReservationDate = input.ReservationDate.Date;
            reservation.DiscountAmount = input.DiscountAmount;
            reservation.ExtraAmount = input.ExtraAmount;
            reservation.Status = input.Status;

            // Repository pattern kullanarak detayları güncelle
            var existingDetails = await _reservationDetailRepository.GetListAsync(x => x.ReservationId == id);
            var existingDetailIds = existingDetails.Select(d => d.Id).ToList();
            var inputDetailIds = input.ReservationDetails.Where(d => d.Id != Guid.Empty).Select(d => d.Id).ToList();

            // Silinecek detayları bul ve kaldır
            var detailsToDelete = existingDetails
                .Where(d => !inputDetailIds.Contains(d.Id))
                .ToList();

            foreach (var detailToDelete in detailsToDelete)
            {
                await _reservationDetailRepository.DeleteAsync(detailToDelete.Id);
            }

            // Detayları güncelle veya ekle
            foreach (var detailDto in input.ReservationDetails)
            {
                if (detailDto.Id != Guid.Empty && existingDetailIds.Contains(detailDto.Id))
                {
                    // Mevcut detayı güncelle
                    var existingDetail = await _reservationDetailRepository.GetAsync(detailDto.Id);
                    existingDetail.ServiceId = detailDto.ServiceId;
                    existingDetail.EmployeeId = detailDto.EmployeeId;
                    existingDetail.StartTime = detailDto.StartTime;
                    existingDetail.ServicePrice = detailDto.ServicePrice;
                    existingDetail.Note = detailDto.Note;

                    await _reservationDetailRepository.UpdateAsync(existingDetail);
                }
                else
                {
                    // Yeni detay ekle
                    var newDetail = await _reservationDetailRepository.InsertAsync(new ReservationDetail
                    {
                        ReservationId = id, // reservation.Id yerine id kullan
                        ServiceId = detailDto.ServiceId,
                        EmployeeId = detailDto.EmployeeId,
                        StartTime = detailDto.StartTime,
                        ServicePrice = detailDto.ServicePrice,
                        Note = detailDto.Note
                    });
                }
            }

            // Fiyatları yeniden hesapla
            await RecalculatePricesAsync(id);

            // Entity'yi güncelle (DTO değil)
            await _reservationRepository.UpdateAsync(reservation);

            // Güncellenmiş DTO'yu döndür
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
            // 1) Çalışma saatleri kontrolü
            var workingHours = await _workingHoursRepository.FindAsync(x =>
                x.EmployeeId == employeeId &&
                x.DayOfWeek == dateTime.DayOfWeek &&
                x.IsActive);

            if (workingHours == null)
            {
                // O gün çalışmıyor
                return false;
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
                    // Mola saatine denk geliyor
                    return false;
                }
            }

            // 2) Aynı gün ve çalışana ait rezervasyon detaylarını SQL tarafında filtrele
            var reservationDetailsQueryable = await _reservationDetailRepository.GetQueryableAsync();

            var todaysDetails = reservationDetailsQueryable
                .Where(x =>
                    x.EmployeeId == employeeId &&
                    x.Reservation.ReservationDate >= dateTime.Date &&
                    x.Reservation.ReservationDate < dateTime.Date.AddDays(1))
                .ToList(); // EF Core burada hem Reservation hem ReservationDate koşullarını SQL'e çevirir

            // 3) Bellek içi çakışma kontrolü
            var hasConflict = todaysDetails.Any(x =>
                !(endTime <= x.StartTime || requestTime >= x.EndTime)
            );

            return !hasConflict;
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

        public async Task<List<CalendarEventDto>> GetCalendarEventsAsync(DateTime startDate, DateTime endDate, Guid? employeeId = null)
        {
            var queryable = await _reservationRepository.GetQueryableAsync();

            queryable = queryable.Where(x =>
                x.ReservationDate >= startDate.Date &&
                x.ReservationDate <= endDate.Date);

            if (employeeId.HasValue)
            {
                queryable = queryable.Where(x =>
                    x.ReservationDetails.Any(d => d.EmployeeId == employeeId.Value));
            }

            var reservations = queryable.ToList();
            var events = new List<CalendarEventDto>();

            foreach (var reservation in reservations)
            {
                var evt = new CalendarEventDto
                {
                    Id = reservation.Id,
                    Start = reservation.ReservationDate.Add(reservation.StartTime),
                    End = reservation.ReservationDate.Add(reservation.EndTime),
                    IsWalkIn = reservation.IsWalkIn,
                    Status = GetReservationStatusDisplay(reservation.Status),
                    TotalPrice = reservation.FinalPrice
                };

                var customer = await _customerRepository.FindAsync(reservation.CustomerId);
                if (customer != null)
                {
                    evt.CustomerName = customer.FullName;
                    evt.CustomerPhone = customer.Phone;
                    evt.Title = customer.FullName;
                }

                var detailList = await _reservationDetailRepository.GetListAsync(x => x.ReservationId == reservation.Id);
                var serviceTitles = new List<string>();
                foreach (var detail in detailList)
                {
                    var service = await _serviceRepository.FindAsync(detail.ServiceId);
                    if (service != null)
                    {
                        serviceTitles.Add(service.Title);
                    }
                }
                evt.Services = string.Join(", ", serviceTitles);

                evt.Color = reservation.IsWalkIn ? "#6c757d" : "#28a745";

                events.Add(evt);
            }

            return events;
        }

        public async Task<List<ReservationDto>> GetTodayReservationsAsync()
        {
            var today = DateTime.Now.Date;
            var list = await _reservationRepository.GetListAsync(x => x.ReservationDate == today);
            var result = new List<ReservationDto>();
            foreach (var reservation in list)
            {
                result.Add(await MapToReservationDtoAsync(reservation));
            }
            return result;
        }

        //todo
        public async Task<List<ReservationDto>> GetUpcomingReservationsAsync(int? hoursAhead = 2)
        {
            var now = DateTime.Now;
            var target = now.AddHours((int)hoursAhead);

            var queryable = await _reservationRepository.GetQueryableAsync();
            var reservations = queryable
                .Where(x =>
                    x.ReservationDate.Add(x.StartTime) >= now &&
                    x.ReservationDate.Add(x.StartTime) <= target)
                .OrderBy(x => x.ReservationDate)
                .ThenBy(x => x.StartTime)
                .ToList();

            var result = new List<ReservationDto>();
            foreach (var reservation in reservations)
            {
                result.Add(await MapToReservationDtoAsync(reservation));
            }

            return result;
        }

        public async Task<List<ReservationDto>> GetCustomerReservationsAsync(Guid customerId, int maxCount = 10)
        {
            var queryable = await _reservationRepository.GetQueryableAsync();
            var reservations = queryable
                .Where(x => x.CustomerId == customerId)
                .OrderByDescending(x => x.ReservationDate)
                .ThenByDescending(x => x.StartTime)
                .Take(maxCount)
                .ToList();

            var result = new List<ReservationDto>();
            foreach (var reservation in reservations)
            {
                result.Add(await MapToReservationDtoAsync(reservation));
            }
            return result;
        }

        public async Task AddServiceToReservationAsync(Guid reservationId, CreateReservationDetailDto serviceDetail)
        {
            var reservation = await _reservationRepository.GetAsync(reservationId);
            var service = await _serviceRepository.GetAsync(serviceDetail.ServiceId);
            var employee = await _employeeRepository.GetAsync(serviceDetail.EmployeeId);

            var dateTime = reservation.ReservationDate.Add(serviceDetail.StartTime);
            var available = await CheckAvailabilityAsync(employee.Id, dateTime, service.DurationMinutes);
            if (!available)
            {
                throw new BusinessException($"{employee.FirstName} {employee.LastName} çalışanı belirtilen saatte müsait değil.");
            }

            var endTime = serviceDetail.StartTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));
            var price = serviceDetail.CustomPrice ?? service.Price;

            var detail = new ReservationDetail
            {
                ReservationId = reservationId,
                ServiceId = serviceDetail.ServiceId,
                EmployeeId = serviceDetail.EmployeeId,
                ServicePrice = price,
                StartTime = serviceDetail.StartTime,
                EndTime = endTime,
                CommissionRate = employee.ServiceCommissionRate,
                CommissionAmount = price * employee.ServiceCommissionRate / 100,
                Note = serviceDetail.Note
            };

            await _reservationDetailRepository.InsertAsync(detail);

            var allDetails = await _reservationDetailRepository.GetListAsync(x => x.ReservationId == reservationId);
            reservation.StartTime = allDetails.Min(x => x.StartTime);
            reservation.EndTime = allDetails.Max(x => x.EndTime);
            await _reservationRepository.UpdateAsync(reservation);

            await RecalculatePricesAsync(reservationId);
        }

        public async Task RemoveServiceFromReservationAsync(Guid reservationId, Guid reservationDetailId)
        {
            await _reservationDetailRepository.DeleteAsync(reservationDetailId);

            var reservation = await _reservationRepository.GetAsync(reservationId);
            var details = await _reservationDetailRepository.GetListAsync(x => x.ReservationId == reservationId);

            if (details.Any())
            {
                reservation.StartTime = details.Min(x => x.StartTime);
                reservation.EndTime = details.Max(x => x.EndTime);
            }
            else
            {
                reservation.StartTime = TimeSpan.Zero;
                reservation.EndTime = TimeSpan.Zero;
            }

            await _reservationRepository.UpdateAsync(reservation);

            await RecalculatePricesAsync(reservationId);
        }

        public async Task UpdateReservationDetailAsync(Guid reservationDetailId, UpdateReservationDetailDto input)
        {
            var detail = await _reservationDetailRepository.GetAsync(reservationDetailId);
            var reservation = await _reservationRepository.GetAsync(detail.ReservationId);
            var service = await _serviceRepository.GetAsync(detail.ServiceId);

            if (input.StartTime != detail.StartTime)
            {
                var dateTime = reservation.ReservationDate.Add(input.StartTime);
                var available = await CheckAvailabilityAsync(detail.EmployeeId, dateTime, service.DurationMinutes);
                if (!available)
                {
                    var employee = await _employeeRepository.GetAsync(detail.EmployeeId);
                    throw new BusinessException($"{employee.FirstName} {employee.LastName} çalışanı belirtilen saatte müsait değil.");
                }

                detail.StartTime = input.StartTime;
                detail.EndTime = input.StartTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));
            }

            if (input.CustomPrice.HasValue)
            {
                detail.ServicePrice = input.CustomPrice.Value;
                detail.CommissionAmount = detail.ServicePrice * detail.CommissionRate / 100;
            }

            detail.Note = input.Note;
            await _reservationDetailRepository.UpdateAsync(detail);

            var details = await _reservationDetailRepository.GetListAsync(x => x.ReservationId == reservation.Id);
            reservation.StartTime = details.Min(x => x.StartTime);
            reservation.EndTime = details.Max(x => x.EndTime);
            await _reservationRepository.UpdateAsync(reservation);

            await RecalculatePricesAsync(reservation.Id);
        }

        public async Task ApplyDiscountAsync(Guid reservationId, decimal discountAmount)
        {
            var reservation = await _reservationRepository.GetAsync(reservationId);
            reservation.DiscountAmount = (reservation.DiscountAmount ?? 0) + discountAmount;
            await _reservationRepository.UpdateAsync(reservation);
            await RecalculatePricesAsync(reservationId);
        }

        public async Task ApplyExtraChargeAsync(Guid reservationId, decimal extraAmount, string reason)
        {
            var reservation = await _reservationRepository.GetAsync(reservationId);
            reservation.ExtraAmount = (reservation.ExtraAmount ?? 0) + extraAmount;
            if (!string.IsNullOrWhiteSpace(reason))
            {
                reservation.Note = string.IsNullOrWhiteSpace(reservation.Note)
                    ? reason
                    : reservation.Note + " | " + reason;
            }
            await _reservationRepository.UpdateAsync(reservation);
            await RecalculatePricesAsync(reservationId);
        }

        public async Task SendReminderAsync(Guid reservationId)
        {
            var reservation = await _reservationRepository.GetAsync(reservationId);
            reservation.ReminderSent = true;
            await _reservationRepository.UpdateAsync(reservation);
        }

        public async Task SendBulkRemindersAsync(DateTime targetDate)
        {
            var list = await _reservationRepository.GetListAsync(x => x.ReservationDate.Date == targetDate.Date && !x.ReminderSent);
            foreach (var reservation in list)
            {
                await SendReminderAsync(reservation.Id);
            }
        }

        public async Task<DailyReservationReportDto> GetDailyReportAsync(DateTime date)
        {
            var reservations = await _reservationRepository.GetListAsync(x => x.ReservationDate.Date == date.Date);
            var report = new DailyReservationReportDto
            {
                Date = date,
                TotalReservations = reservations.Count,
                CompletedReservations = reservations.Count(x => x.Status == ReservationStatus.Arrived),
                NoShowReservations = reservations.Count(x => x.Status == ReservationStatus.NoShow),
                WalkInReservations = reservations.Count(x => x.IsWalkIn),
                TotalRevenue = reservations.Sum(x => x.FinalPrice),
                AverageReservationValue = reservations.Any() ? reservations.Average(x => x.FinalPrice) : 0
            };

            var ids = reservations.Select(r => r.Id).ToList();
            var details = await _reservationDetailRepository.GetListAsync(x => ids.Contains(x.ReservationId));
            var top = details.GroupBy(x => x.ServiceId)
                .Select(g => new { ServiceId = g.Key, Count = g.Count(), Revenue = g.Sum(d => d.ServicePrice) })
                .OrderByDescending(g => g.Count)
                .Take(5)
                .ToList();

            foreach (var item in top)
            {
                var service = await _serviceRepository.FindAsync(item.ServiceId);
                report.TopServices.Add(new ServicePerformanceDto
                {
                    ServiceId = item.ServiceId,
                    ServiceName = service?.Title,
                    Count = item.Count,
                    Revenue = item.Revenue
                });
            }

            return report;
        }

        public async Task<EmployeePerformanceReportDto> GetEmployeePerformanceAsync(Guid employeeId, DateTime startDate, DateTime endDate)
        {
            var employee = await _employeeRepository.GetAsync(employeeId);
            var details = await _reservationDetailRepository.GetListAsync(x => x.EmployeeId == employeeId);
            details = details.Where(d =>
            {
                var reservation = _reservationRepository.FindAsync(d.ReservationId).Result;
                return reservation.ReservationDate >= startDate.Date && reservation.ReservationDate <= endDate.Date;
            }).ToList();

            var report = new EmployeePerformanceReportDto
            {
                EmployeeId = employeeId,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                TotalReservations = details.Count,
                TotalRevenue = details.Sum(x => x.ServicePrice),
                TotalCommission = details.Sum(x => x.CommissionAmount)
            };

            var groups = details.GroupBy(x => x.ServiceId);
            foreach (var g in groups)
            {
                var service = await _serviceRepository.FindAsync(g.Key);
                report.Services.Add(new ServicePerformanceDto
                {
                    ServiceId = g.Key,
                    ServiceName = service?.Title,
                    Count = g.Count(),
                    Revenue = g.Sum(d => d.ServicePrice)
                });
            }

            return report;
        }


        public async Task<ReservationDto> CompleteReservationAsync(CompleteReservationDto input)
        {
            using var uow = UnitOfWorkManager.Begin();

            try
            {
                var reservation = await _reservationRepository.GetAsync(input.ReservationId);

                // 1. Hizmet fiyat değişikliklerini uygula
                if (input.ServiceChanges?.Any() == true)
                {
                    foreach (var change in input.ServiceChanges)
                    {
                        var detail = await _reservationDetailRepository.GetAsync(change.ReservationDetailId);

                        if (Math.Abs(detail.ServicePrice - change.NewPrice) > 0.01m) // Fiyat değişmişse
                        {
                            detail.ServicePrice = change.NewPrice;

                            // Komisyon yeniden hesapla
                            detail.CommissionAmount = detail.ServicePrice * detail.CommissionRate / 100;

                            await _reservationDetailRepository.UpdateAsync(detail);
                        }
                    }
                }

                // 2. İndirim ve ek ücret güncelle
                reservation.DiscountAmount = input.AdditionalDiscount > 0 ? input.AdditionalDiscount : null;
                reservation.ExtraAmount = input.AdditionalCharge > 0 ? input.AdditionalCharge : null;

                // 3. Fiyatları yeniden hesapla
                await RecalculatePricesAsync(input.ReservationId);

                // 4. Rezervasyon durumunu güncelle
                reservation.Status = ReservationStatus.Arrived;
                await _reservationRepository.UpdateAsync(reservation);

                // 5. Ödeme kaydet (eğer tutar > 0 ise)
                if (input.PaymentAmount > 0)
                {
                    var paymentDto = new CreateReservationPaymentDto
                    {
                        ReservationId = input.ReservationId,
                        Amount = input.PaymentAmount,
                        PaymentMethod = input.PaymentMethod,
                        Description = input.PaymentNote ?? $"Rezervasyon tamamlama ödemesi - {DateTime.Now:dd.MM.yyyy HH:mm}"
                    };

                    await _paymentAppService.CreateReservationPaymentAsync(paymentDto);
                }

                await uow.CompleteAsync();

                return await GetAsync(input.ReservationId);
            }
            catch (Exception)
            {
                await uow.RollbackAsync();
                throw;
            }
        }

    }
}