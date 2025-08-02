using AutoMapper.Internal.Mappers;
using BeroxAppy.Customers;
using BeroxAppy.Enums;
using BeroxAppy.Finance;
using BeroxAppy.Finances;
using BeroxAppy.Reservations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp;

namespace BeroxAppy.Services
{
    public class PaymentAppService : ApplicationService, IPaymentAppService
    {
        private readonly IRepository<Payment, Guid> _paymentRepository;
        private readonly IRepository<Customer, Guid> _customerRepository;
        private readonly IRepository<Reservation, Guid> _reservationRepository;
        private readonly IRepository<CashRegister, Guid> _cashRegisterRepository;
        private readonly Lazy<IReservationAppService> _reservationAppService;

        public PaymentAppService(
            IRepository<Payment, Guid> paymentRepository,
            IRepository<Customer, Guid> customerRepository,
            IRepository<Reservation, Guid> reservationRepository,
            IRepository<CashRegister, Guid> cashRegisterRepository,
            Lazy<IReservationAppService> reservationAppService)
        {
            _paymentRepository = paymentRepository;
            _customerRepository = customerRepository;
            _reservationRepository = reservationRepository;
            _cashRegisterRepository = cashRegisterRepository;
            _reservationAppService = reservationAppService;
        }

        // Günlük kasa oluştur veya getir
        public async Task<CashRegister> GetOrCreateTodaysCashRegisterAsync()
        {
            var today = DateTime.Now.Date;
            var cashRegister = await _cashRegisterRepository.FindAsync(x => x.Date == today);

            if (cashRegister == null)
            {
                // Dünkü kasayı bul
                var yesterday = today.AddDays(-1);
                var yesterdayCash = await _cashRegisterRepository
                    .FindAsync(x => x.Date == yesterday && x.IsClosed);

                cashRegister = new CashRegister
                {
                    Date = today,
                    OpeningBalance = yesterdayCash?.ClosingBalance ?? 0,
                    ClosingBalance = 0,
                    TotalCashIn = 0,
                    TotalCashOut = 0,
                    IsClosed = false,
                    Note = $"Otomatik oluşturuldu - {today:dd.MM.yyyy}" 
                };

                await _cashRegisterRepository.InsertAsync(cashRegister);
            }

            return cashRegister;
        }

        public async Task<PaymentDto> CreateAsync(CreatePaymentDto input)
        {

            // Nakit ödeme ise kasaya ekle
            Guid? cashRegisterId = null;
            if (input.PaymentMethod == PaymentMethod.Cash)
            {
                var cashRegister = await GetOrCreateTodaysCashRegisterAsync();
                cashRegisterId = cashRegister.Id;

                // Kasa bakiyesini güncelle
                if (input.IsRefund)
                {
                    cashRegister.TotalCashOut += input.Amount;
                }
                else
                {
                    cashRegister.TotalCashIn += input.Amount;
                }

                await _cashRegisterRepository.UpdateAsync(cashRegister);
            }

            var payment = new Payment
            {
                CustomerId = input.CustomerId,
                ReservationId = input.ReservationId,
                Amount = input.Amount,
                PaymentMethod = input.PaymentMethod,
                PaymentDate = input.PaymentDate,
                Description = input.Description,
                IsRefund = input.IsRefund,
                CashRegisterId = cashRegisterId // Burada set ediyoruz

            };

            await _paymentRepository.InsertAsync(payment);


            // Rezervasyon ödemesi ise ödeme durumunu güncelle
            if (input.ReservationId.HasValue)
            {
                await UpdateReservationPaymentStatusAsync(input.ReservationId.Value);
            }

            // Müşteri borcunu güncelle
            await UpdateCustomerDebtAsync(input.CustomerId, input.Amount, true);

            return await MapToPaymentDtoAsync(payment);
        }

        // Kasa kapatma
        public async Task<CashRegisterDto> CloseCashRegisterAsync(Guid cashRegisterId, decimal actualClosingBalance, string note = null)
        {
            var cashRegister = await _cashRegisterRepository.GetAsync(cashRegisterId);

            if (cashRegister.IsClosed)
            {
                throw new UserFriendlyException("Bu kasa zaten kapatılmış!");
            }

            // Teorik bakiye hesapla
            var theoreticalBalance = cashRegister.OpeningBalance + cashRegister.TotalCashIn - cashRegister.TotalCashOut;

            cashRegister.ClosingBalance = actualClosingBalance;
            cashRegister.IsClosed = true;
            cashRegister.Note = note;

            await _cashRegisterRepository.UpdateAsync(cashRegister);

            var dto = ObjectMapper.Map<CashRegister, CashRegisterDto>(cashRegister);
            dto.TheoreticalBalance = theoreticalBalance;
            dto.Difference = actualClosingBalance - theoreticalBalance;

            return dto;
        }

        // Günlük kasa raporu
        public async Task<DailyCashReportDto> GetDailyCashReportAsync(DateTime date)
        {
            var cashRegister = await _cashRegisterRepository.FindAsync(x => x.Date == date.Date);

            if (cashRegister == null)
            {
                return new DailyCashReportDto { Date = date, HasData = false };
            }

            var payments = await _paymentRepository.GetListAsync(x =>
                x.CashRegisterId == cashRegister.Id);

            var report = new DailyCashReportDto
            {
                Date = date,
                HasData = true,
                OpeningBalance = cashRegister.OpeningBalance,
                TotalCashIn = cashRegister.TotalCashIn,
                TotalCashOut = cashRegister.TotalCashOut,
                TheoreticalClosing = cashRegister.OpeningBalance + cashRegister.TotalCashIn - cashRegister.TotalCashOut,
                ActualClosing = cashRegister.ClosingBalance,
                IsClosed = cashRegister.IsClosed,
                Payments = payments.Select(p => ObjectMapper.Map<Payment, PaymentDto>(p)).ToList()
            };

            return report;
        }

        public async Task<PaymentDto> CreateReservationPaymentAsync(CreateReservationPaymentDto input)
        {

            // 1. Rezervasyon validation
            var reservation = await _reservationRepository.GetAsync(input.ReservationId);

            if (reservation.Status == ReservationStatus.NoShow)
            {
                throw new UserFriendlyException("Gelmedi olarak işaretlenmiş rezervasyona ödeme yapılamaz!");
            }

            // 2. Ödeme tutarı kontrolü 
            var currentPaidAmount = await GetReservationPaidAmountAsync(input.ReservationId);
            var remainingAmount = reservation.FinalPrice - currentPaidAmount;

            if (input.Amount > remainingAmount + 0.01m)
            {
                throw new UserFriendlyException($"Ödeme tutarı kalan borçtan ({remainingAmount:C}) fazla olamaz!");
            }

            if (input.Amount <= 0)
            {
                throw new UserFriendlyException("Ödeme tutarı sıfırdan büyük olmalıdır!");
            }

            Guid? cashRegisterId = input.CashRegisterId; 

            if (input.PaymentMethod == PaymentMethod.Cash)
            {
                var todaysCashRegister = await GetOrCreateTodaysCashRegisterAsync();
                cashRegisterId = todaysCashRegister.Id;

                // Kasa kapanmış mı kontrol et
                if (todaysCashRegister.IsClosed)
                {
                    throw new UserFriendlyException("Günlük kasa kapatılmış! Nakit ödeme yapılamaz.");
                }
                // Kasa bakiyesini güncelle
                todaysCashRegister.TotalCashIn += input.Amount;
                await _cashRegisterRepository.UpdateAsync(todaysCashRegister);
            }

            // 4. Payment entity oluştur
            var payment = new Payment
            {
                CustomerId = reservation.CustomerId,
                ReservationId = input.ReservationId,
                Amount = input.Amount,
                PaymentMethod = input.PaymentMethod,
                PaymentDate = DateTime.Now,
                Description = string.IsNullOrWhiteSpace(input.Description)
                    ? $"Rezervasyon ödemesi - {reservation.ReservationDate:dd.MM.yyyy}"
                    : input.Description,
                IsRefund = false,
                CashRegisterId = cashRegisterId
            };
            await _paymentRepository.InsertAsync(payment);



            // Rezervasyon durumlarını güncelle
            await UpdateReservationPaymentStatusAsync(input.ReservationId);

            // Eğer tam ödeme yapıldıysa rezervasyonu tamamla
            var newPaidAmount = await GetReservationPaidAmountAsync(input.ReservationId);
            if ((newPaidAmount + payment.Amount) >= reservation.FinalPrice)
            {
                await _reservationAppService.Value.UpdatePaymentStatusAsync(input.ReservationId, PaymentStatus.Paid);
            }
            else if (newPaidAmount > 0)
            {
                await _reservationAppService.Value.UpdatePaymentStatusAsync(input.ReservationId, PaymentStatus.Partial);
            }

            // Müşteri borcunu güncelle (borcu azalt)
            bool borcKontrol = (reservation.FinalPrice - input.Amount) == 0;
            await UpdateCustomerDebtAsync(reservation.CustomerId, input.Amount, borcKontrol);

            return await MapToPaymentDtoAsync(payment);
        }

        public async Task<PaymentDto> GetAsync(Guid id)
        {
            var payment = await _paymentRepository.GetAsync(id);
            return await MapToPaymentDtoAsync(payment);
        }

        public async Task<PagedResultDto<PaymentDto>> GetListAsync(GetPaymentsInput input)
        {
            var queryable = await _paymentRepository.GetQueryableAsync();

            // Filtreleme
            if (input.CustomerId.HasValue)
            {
                queryable = queryable.Where(x => x.CustomerId == input.CustomerId);
            }

            if (input.ReservationId.HasValue)
            {
                queryable = queryable.Where(x => x.ReservationId == input.ReservationId);
            }

            if (input.PaymentMethod.HasValue)
            {
                queryable = queryable.Where(x => x.PaymentMethod == input.PaymentMethod);
            }

            if (input.StartDate.HasValue)
            {
                queryable = queryable.Where(x => x.PaymentDate >= input.StartDate.Value.Date);
            }

            if (input.EndDate.HasValue)
            {
                queryable = queryable.Where(x => x.PaymentDate <= input.EndDate.Value.Date.AddDays(1));
            }

            if (input.IsRefund.HasValue)
            {
                queryable = queryable.Where(x => x.IsRefund == input.IsRefund);
            }

            if (input.MinAmount.HasValue)
            {
                queryable = queryable.Where(x => x.Amount >= input.MinAmount);
            }

            if (input.MaxAmount.HasValue)
            {
                queryable = queryable.Where(x => x.Amount <= input.MaxAmount);
            }

            // Sıralama
            queryable = queryable.OrderByDescending(x => x.PaymentDate);

            var totalCount = queryable.Count();
            var payments = queryable
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .ToList();

            var dtos = new List<PaymentDto>();
            foreach (var payment in payments)
            {
                var dto = await MapToPaymentDtoAsync(payment);
                dtos.Add(dto);
            }

            return new PagedResultDto<PaymentDto>(totalCount, dtos);
        }

        public async Task<ListResultDto<PaymentDto>> GetReservationPaymentsAsync(Guid reservationId)
        {
            var payments = await _paymentRepository.GetListAsync(x => x.ReservationId == reservationId);
            var dtos = new List<PaymentDto>();

            foreach (var payment in payments.OrderBy(x => x.PaymentDate))
            {
                var dto = await MapToPaymentDtoAsync(payment);
                dtos.Add(dto);
            }

            return new ListResultDto<PaymentDto>(dtos);
        }

        public async Task<ListResultDto<PaymentDto>> GetCustomerPaymentsAsync(Guid customerId)
        {
            var payments = await _paymentRepository.GetListAsync(x => x.CustomerId == customerId);
            var dtos = new List<PaymentDto>();

            foreach (var payment in payments.OrderByDescending(x => x.PaymentDate))
            {
                var dto = await MapToPaymentDtoAsync(payment);
                dtos.Add(dto);
            }

            return new ListResultDto<PaymentDto>(dtos);
        }

        public async Task<decimal> GetReservationPaidAmountAsync(Guid reservationId)
        {
            var payments = await _paymentRepository.GetListAsync(x =>
                x.ReservationId == reservationId && !x.IsRefund);

            var refunds = await _paymentRepository.GetListAsync(x =>
                x.ReservationId == reservationId && x.IsRefund);

            return payments.Sum(x => x.Amount) - refunds.Sum(x => x.Amount);
        }

        public async Task<decimal> GetReservationRemainingAmountAsync(Guid reservationId)
        {
            var reservation = await _reservationRepository.GetAsync(reservationId);
            var paidAmount = await GetReservationPaidAmountAsync(reservationId);
            return Math.Max(0, reservation.FinalPrice - paidAmount);
        }

        public async Task DeleteAsync(Guid id)
        {
            var payment = await _paymentRepository.GetAsync(id);

            // Ödemeyi sil
            await _paymentRepository.DeleteAsync(payment);

            // İlgili durumları güncelle
            if (payment.ReservationId.HasValue)
            {
                await UpdateReservationPaymentStatusAsync(payment.ReservationId.Value);
            }

            // Müşteri borcunu güncelle (tersi işlem)
            await UpdateCustomerDebtAsync(payment.CustomerId, payment.Amount, payment.IsRefund);
        }




        private async Task<PaymentDto> MapToPaymentDtoAsync(Payment payment)
        {
            var dto = ObjectMapper.Map<Payment, PaymentDto>(payment);

            // Customer bilgileri
            var customer = await _customerRepository.FindAsync(payment.CustomerId);
            if (customer != null)
            {
                dto.CustomerName = customer.FullName;
                dto.CustomerPhone = customer.Phone;
            }

            // Reservation bilgileri
            if (payment.ReservationId.HasValue)
            {
                var reservation = await _reservationRepository.FindAsync(payment.ReservationId.Value);
                if (reservation != null)
                {
                    dto.ReservationDate = reservation.ReservationDate;
                    dto.ReservationDisplay = $"{reservation.ReservationDate:dd.MM.yyyy} - {reservation.StartTime:hh\\:mm}";
                }
            }

            // Display alanları
            dto.PaymentMethodDisplay = GetPaymentMethodDisplay(payment.PaymentMethod);
            dto.PaymentTypeDisplay = payment.IsRefund ? "İade" : "Ödeme";
            dto.AmountDisplay = payment.IsRefund
                ? $"-₺{payment.Amount:N2}"
                : $"₺{payment.Amount:N2}";

            return dto;
        }

        private async Task UpdateReservationPaymentStatusAsync(Guid reservationId)
        {
            var reservation = await _reservationRepository.GetAsync(reservationId);
            var paidAmount = await GetReservationPaidAmountAsync(reservationId);

            PaymentStatus newStatus;
            if (paidAmount <= 0)
            {
                newStatus = PaymentStatus.Pending;
            }
            else if (paidAmount >= reservation.FinalPrice)
            {
                newStatus = PaymentStatus.Paid;
            }
            else
            {
                newStatus = PaymentStatus.Partial;
            }

            if (reservation.PaymentStatus != newStatus)
            {
                await _reservationAppService.Value.UpdatePaymentStatusAsync(reservationId, newStatus);
            }
        }

        private async Task UpdateCustomerDebtAsync(Guid customerId, decimal amount, bool isDecrease) //azalma mi
        {
            var customer = await _customerRepository.GetAsync(customerId);

            if (isDecrease)
            {
                customer.TotalDebt = Math.Max(0, customer.TotalDebt - amount);
            }
            else
            {
                customer.TotalDebt += amount;
            }

            await _customerRepository.UpdateAsync(customer);
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
}
