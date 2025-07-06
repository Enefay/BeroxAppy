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
        Cash = 0,
        CreditCard = 1,
        DebitCard = 2,
        Transfer = 3,
        Other = 4
    }

    public enum PaymentStatus
    {
        Pending = 0,
        Partial = 1,
        Paid = 2,
        Refunded = 3
    }
}
