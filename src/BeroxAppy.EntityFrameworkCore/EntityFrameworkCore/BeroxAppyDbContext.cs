using BeroxAppy.Customers;
using BeroxAppy.Employees;
using BeroxAppy.Finance;
using BeroxAppy.Notifications;
using BeroxAppy.Reservations;
using BeroxAppy.Sells;
using BeroxAppy.Services;
using BeroxAppy.TenantSettings;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;

namespace BeroxAppy.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class BeroxAppyDbContext :
    AbpDbContext<BeroxAppyDbContext>,
    IIdentityDbContext,
    ITenantManagementDbContext
{
    /* Add DbSet properties for your Aggregate Roots / Entities here. */

    #region Entities from the modules

    /* Notice: We only implemented IIdentityDbContext and ITenantManagementDbContext
     * and replaced them for this DbContext. This allows you to perform JOIN
     * queries for the entities of these modules over the repositories easily. You
     * typically don't need that for other modules. But, if you need, you can
     * implement the DbContext interface of the needed module and use ReplaceDbContext
     * attribute just like IIdentityDbContext and ITenantManagementDbContext.
     *
     * More info: Replacing a DbContext of a module ensures that the related module
     * uses this DbContext on runtime. Otherwise, it will use its own DbContext class.
     */

    //Identity
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }
    // Tenant Management
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }

    #endregion



    public DbSet<Service> Services { get; set; }
    public DbSet<ServiceCategory> ServiceCategories { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<EmployeeService> EmployeeServices { get; set; }
    public DbSet<EmployeeWorkingHours> EmployeeWorkingHours { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<ReservationDetail> ReservationDetails { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<CashRegister> CashRegisters { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductSale> ProductSales { get; set; }
    public DbSet<NotificationHistory> NotificationHistories { get; set; }
    public DbSet<ReservationSetting> ReservationSettings { get; set; }

    public DbSet<EmployeeCommission> EmployeeCommissions { get; set; }
    public DbSet<EmployeePayment> EmployeePayments { get; set; }
    public DbSet<DailyFinancialSummary> DailyFinancialSummaries { get; set; }

    //DailyFinancialSummary

    public BeroxAppyDbContext(DbContextOptions<BeroxAppyDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureFeatureManagement();
        builder.ConfigureTenantManagement();

        /* Configure your own tables/entities inside here */

        //builder.Entity<YourEntity>(b =>
        //{
        //    b.ToTable(BeroxAppyConsts.DbTablePrefix + "YourEntities", BeroxAppyConsts.DbSchema);
        //    b.ConfigureByConvention(); //auto configure for the base class props
        //    //...
        //});
    }
}
