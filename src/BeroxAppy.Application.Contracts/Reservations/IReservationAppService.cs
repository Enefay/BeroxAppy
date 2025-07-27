using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BeroxAppy.Enums;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace BeroxAppy.Reservations
{
    public interface IReservationAppService : IApplicationService
    {

        /// <summary>
        /// Rezervasyon listesi getir
        /// </summary>
        Task<PagedResultDto<ReservationDto>> GetListAsync(GetReservationsInput input);

        /// <summary>
        /// Rezervasyon detayı getir
        /// </summary>
        Task<ReservationDto> GetAsync(Guid id);

        /// <summary>
        /// Yeni rezervasyon oluştur
        /// </summary>
        Task<ReservationDto> CreateAsync(CreateReservationDto input);

        /// <summary>
        /// Rezervasyon güncelle
        /// </summary>
        Task<ReservationDto> UpdateAsync(Guid id, UpdateReservationDto input);

        /// <summary>
        /// Rezervasyon sil
        /// </summary>
        Task DeleteAsync(Guid id);

        // =============== DURUM YÖNETİMİ ===============

        /// <summary>
        /// Rezervasyon durumunu güncelle
        /// </summary>
        Task UpdateStatusAsync(Guid id, ReservationStatus status);

        /// <summary>
        /// Ödeme durumunu güncelle
        /// </summary>
        Task UpdatePaymentStatusAsync(Guid id, PaymentStatus paymentStatus);

        /// <summary>
        /// Müşteri geldi işaretle
        /// </summary>
        Task MarkAsArrivedAsync(Guid id);

        /// <summary>
        /// Müşteri gelmedi işaretle
        /// </summary>
        Task MarkAsNoShowAsync(Guid id);

        // =============== MÜSAİTLİK KONTROLÜ ===============

        /// <summary>
        /// Belirli tarih/saat için müsaitlik kontrol et
        /// </summary>
        Task<bool> CheckAvailabilityAsync(Guid employeeId, DateTime dateTime, int durationMinutes);

        /// <summary>
        /// Çalışan için müsait saatleri getir
        /// </summary>
        Task<AvailabilityCheckDto> GetAvailableSlotsAsync(Guid employeeId, Guid serviceId, DateTime date);

        /// <summary>
        /// Hizmet için müsait çalışanları getir
        /// </summary>
        Task<List<Guid>> GetAvailableEmployeesAsync(Guid serviceId, DateTime dateTime, int durationMinutes);

        // =============== TAKVİM VE LİSTELEME ===============

        /// <summary>
        /// Takvim görünümü için etkinlikler
        /// </summary>
        Task<List<CalendarEventDto>> GetCalendarEventsAsync(DateTime startDate, DateTime endDate, Guid? employeeId = null);

        /// <summary>
        /// Bugünkü rezervasyonlar
        /// </summary>
        Task<List<ReservationDto>> GetTodayReservationsAsync();

        /// <summary>
        /// Yaklaşan rezervasyonlar (1-2 saat içinde)
        /// </summary>
        Task<List<ReservationDto>> GetUpcomingReservationsAsync(int? hoursAhead = 2);

        /// <summary>
        /// Müşterinin rezervasyon geçmişi
        /// </summary>
        Task<List<ReservationDto>> GetCustomerReservationsAsync(Guid customerId, int maxCount = 10);

        // =============== REZERVASYON DETAYI YÖNETİMİ ===============

        /// <summary>
        /// Rezervasyona hizmet ekle
        /// </summary>
        Task AddServiceToReservationAsync(Guid reservationId, CreateReservationDetailDto serviceDetail);

        /// <summary>
        /// Rezervasyondan hizmet kaldır
        /// </summary>
        Task RemoveServiceFromReservationAsync(Guid reservationId, Guid reservationDetailId);

        /// <summary>
        /// Hizmet detayını güncelle
        /// </summary>
        Task UpdateReservationDetailAsync(Guid reservationDetailId, UpdateReservationDetailDto input);

        // =============== FİYAT HESAPLAMA ===============

        /// <summary>
        /// Rezervasyon fiyatını yeniden hesapla
        /// </summary>
        Task RecalculatePricesAsync(Guid reservationId);

        /// <summary>
        /// İndirim uygula
        /// </summary>
        Task ApplyDiscountAsync(Guid reservationId, decimal discountAmount);

        /// <summary>
        /// Ekstra ücret ekle
        /// </summary>
        Task ApplyExtraChargeAsync(Guid reservationId, decimal extraAmount, string reason);

        // =============== BİLDİRİM VE HATIRLATMA ===============

        /// <summary>
        /// Hatırlatma SMS'i gönder
        /// </summary>
        Task SendReminderAsync(Guid reservationId);

        /// <summary>
        /// Toplu hatırlatma gönder (yarınki rezervasyonlar)
        /// </summary>
        Task SendBulkRemindersAsync(DateTime targetDate);

        // =============== RAPORLAMA ===============

        /// <summary>
        /// Günlük rezervasyon raporu
        /// </summary>
        Task<DailyReservationReportDto> GetDailyReportAsync(DateTime date);

        /// <summary>
        /// Çalışan performans raporu
        /// </summary>
        Task<EmployeePerformanceReportDto> GetEmployeePerformanceAsync(Guid employeeId, DateTime startDate, DateTime endDate);


        /// <summary>
        /// Rezervasyon tamamlama
        /// </summary>
        Task<ReservationDto> CompleteReservationAsync(CompleteReservationDto input);
    }

    // UpdateReservationDetailDto.cs
    public class UpdateReservationDetailDto
    {
        public TimeSpan StartTime { get; set; }
        public decimal? CustomPrice { get; set; }
        public string Note { get; set; }
    }

    // Rapor DTO'ları
    public class DailyReservationReportDto
    {
        public DateTime Date { get; set; }
        public int TotalReservations { get; set; }
        public int CompletedReservations { get; set; }
        public int NoShowReservations { get; set; }
        public int WalkInReservations { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageReservationValue { get; set; }
        public List<ServicePerformanceDto> TopServices { get; set; } = new();
    }

    public class ServicePerformanceDto
    {
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; }
        public int Count { get; set; }
        public decimal Revenue { get; set; }
    }

    public class EmployeePerformanceReportDto
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public int TotalReservations { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCommission { get; set; }
        public List<ServicePerformanceDto> Services { get; set; } = new();
    }
}