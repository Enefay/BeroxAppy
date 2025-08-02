using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BeroxAppy.Finance
{
    public class DailyFinancialSummary : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }

        public DateTime Date { get; set; }

        // GELİRLER
        public decimal ServiceIncome { get; set; } = 0;      // Hizmet gelirleri
        public decimal ProductIncome { get; set; } = 0;      // Ürün satış gelirleri  
        public decimal OtherIncome { get; set; } = 0;        // Diğer gelirler
        public decimal TotalIncome { get; set; } = 0;        // Toplam gelir

        // GİDERLER
        public decimal EmployeePayments { get; set; } = 0;   // Personel ödemeleri
        public decimal OperationalExpenses { get; set; } = 0; // İşletme giderleri
        public decimal OtherExpenses { get; set; } = 0;      // Diğer giderler
        public decimal TotalExpenses { get; set; } = 0;      // Toplam gider

        // NET KAR/ZARAR
        public decimal NetProfit { get; set; } = 0;          // Net kar (Gelir - Gider)

        // ÖDEME YÖNTEMLERİ DAĞILIMI
        public decimal CashAmount { get; set; } = 0;         // Nakit
        public decimal CreditCardAmount { get; set; } = 0;   // Kredi kartı
        public decimal DebitCardAmount { get; set; } = 0;    // Banka kartı
        public decimal BankTransferAmount { get; set; } = 0; // Havale/EFT

        // İSTATİSTİKLER
        public int TotalReservations { get; set; } = 0;      // Toplam rezervasyon sayısı
        public int CompletedReservations { get; set; } = 0;   // Tamamlanan rezervasyon sayısı
        public int CancelledReservations { get; set; } = 0;  // İptal edilen rezervasyon sayısı
        public decimal AverageTicketSize { get; set; } = 0;  // Ortalama işlem tutarı

        // GÜNLÜK DURUM
        public bool IsClosed { get; set; } = false;          // Gün kapatıldı mı?
        public DateTime? ClosedDate { get; set; }            // Gün kapanış tarihi
        public Guid? ClosedByUserId { get; set; }            // Kim tarafından kapatıldı

        [MaxLength(1000)]
        public string? Note { get; set; }                     // Günlük notlar

        // Hesaplanan property'ler
        public decimal TotalCashFlow => TotalIncome - TotalExpenses;
        public decimal ProfitMargin => TotalIncome > 0 ? (NetProfit / TotalIncome) * 100 : 0;
    }
}
