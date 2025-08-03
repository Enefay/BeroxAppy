using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeroxAppy.Enums
{
    public enum Gender
    {
        Unisex = 0,
        Male = 1,
        Female = 2
    }

    public enum EmployeeType
    {
        Staff = 0,
        Secretary = 1,
        Manager = 2,
        Device = 3 // Cihaz (epilasyon cihazı vb. için)
    }

    public enum ReservationStatus
    {
        Pending = 0,      // Beklemede
        NoShow = 1,       // Gelmedi
        Arrived = 2       // Geldi
    }

    public enum PaymentMethod
    {
        Cash = 0, //Nakit
        CreditCard = 1, //Kredi Kartı
        DebitCard = 2, //Banka Kartı
        BankTransfer = 3, //Havale/EFT
        Check = 4, //Çek
        Other = 5 //Diğer
    }

    public enum PaymentStatus
    {
        Pending = 0, //beklemede
        Partial = 1, //kısmi
        Paid = 2, //odendi
        Refunded = 3 //iade
    }

    //Finans
    public enum TransactionType
    {
        Income = 1, //gelir
        Expense = 2 //gider
    }

    public enum SalaryPeriodType //maaş ödeme zamanı
    {
        Daily = 1, //gunluk
        Weekly = 2, //haftalık
        BiWeekly = 3, // 2 haftalık
        Monthly = 4 //aylık
    }


    public enum CommissionType // komisyon tipi
    {
        Service = 1, //hizmet
        Product = 2, //ürün
        Package = 3, //paket
    }

    public enum PaymentType
    {
        Salary = 1,     // Maaş
        Commission = 2, // Komisyon
        Bonus = 3,       // Prim/Kesinti
        Advance = 4      // Avans (ileride eklenebilir)
    }
}
