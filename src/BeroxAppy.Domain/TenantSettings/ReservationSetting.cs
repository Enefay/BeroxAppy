using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace BeroxAppy.TenantSettings
{
    public class ReservationSetting : Entity<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }

        public int ReservationIntervalMinutes { get; set; } = 15;

        public bool SendReminder { get; set; } = true;
        public int ReminderHoursBefore { get; set; } = 24;

        public bool SendCreatedNotification { get; set; } = true;

        public bool AllowOnlineReservation { get; set; } = true;

        public int MinHoursBeforeOnlineReservation { get; set; } = 2;
        public int MaxDaysForOnlineReservation { get; set; } = 30;

        // İşletme çalışma saatleri
        public TimeSpan DefaultStartTime { get; set; } = new TimeSpan(9, 0, 0);
        public TimeSpan DefaultEndTime { get; set; } = new TimeSpan(18, 0, 0);
    }
}
