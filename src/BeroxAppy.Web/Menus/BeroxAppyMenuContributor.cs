using System.Threading.Tasks;
using BeroxAppy.Localization;
using BeroxAppy.MultiTenancy;
using Volo.Abp.Identity.Web.Navigation;
using Volo.Abp.SettingManagement.Web.Navigation;
using Volo.Abp.TenantManagement.Web.Navigation;
using Volo.Abp.UI.Navigation;

namespace BeroxAppy.Web.Menus;

public class BeroxAppyMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
    }

    private Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        var administration = context.Menu.GetAdministration();
        var l = context.GetLocalizer<BeroxAppyResource>();

        context.Menu.Items.Insert(
            0,
            new ApplicationMenuItem(
                BeroxAppyMenus.Home,
                l["Menu:Home"],
                "~/",
                icon: "fas fa-home",
                order: 0
            )
        );


        // Hizmetler Ana Menüsü
        var servicesMenu = new ApplicationMenuItem(
            "BeroxAppy.Services",
            "Hizmetler",
            icon: "fas fa-cogs"
        );

        // Hizmet Kategorileri
        servicesMenu.AddItem(new ApplicationMenuItem(
            "BeroxAppy.ServiceCategories",
            "Hizmet Kategorileri",
            url: "/Services/ServiceCategories",
            icon: "fas fa-tags"
        ));

        // Hizmetler
        servicesMenu.AddItem(new ApplicationMenuItem(
            "BeroxAppy.Services",
            "Hizmetler",
            url: "/Services/Services",
            icon: "fas fa-cut"
        ));

        context.Menu.AddItem(servicesMenu);

        // Müşteriler Menüsü 
        var customersMenu = new ApplicationMenuItem(
            "BeroxAppy.Customers",
            "Müşteriler",
            url: "/Customers",
            icon: "fas fa-users"
        );
        context.Menu.AddItem(customersMenu);

        // Çalışanlar Menüsü - YENİ!
        var employeesMenu = new ApplicationMenuItem(
            "BeroxAppy.Employees",
            "Çalışanlar",
            url: "/Employees",
            icon: "fas fa-users-cog"
        );




        context.Menu.AddItem(employeesMenu);


        // Randevular Menüsü
        var reservationsMenu = new ApplicationMenuItem(
            BeroxAppyMenus.Reservations,
            "Randevular",
            url: "/Reservations",
            icon: "fas fa-calendar-alt"
        );
        var walkInsMenu = new ApplicationMenuItem(
          BeroxAppyMenus.WalkIns,
          "Adisyonlar",
          url: "/Reservations/WalkIns",
          icon: "fas fa-cash-register"
        );


        context.Menu.AddItem(reservationsMenu);
        context.Menu.AddItem(walkInsMenu);


        // Finance menüsü
        var financeMenu = new ApplicationMenuItem(
            BeroxAppyMenus.Finance,
            "Finans",
            "/Finance/Dashboard",
            icon: "fas fa-chart-line"
        );

        financeMenu.AddItem(new ApplicationMenuItem(
            BeroxAppyMenus.FinanceDashboard,
            "Dashboard",
            "/Finance/Dashboard",
            icon: "fas fa-tachometer-alt"
        ));

        financeMenu.AddItem(new ApplicationMenuItem(
           BeroxAppyMenus.FinanceCommission,
           "Komisyonlar",
           "/Finance/Commissions",
           icon: "fas fa-money-bill-wave"
       ));

        financeMenu.AddItem(new ApplicationMenuItem(
         BeroxAppyMenus.FinanceCommission,
         "Kasa Yönetimi",
         "/Finance/CashRegister",
         icon: "fas fa-cash-register"
         ));

        // MAAŞ ÖDEMELERİ
        financeMenu.AddItem(new ApplicationMenuItem(
              BeroxAppyMenus.FinanceSalaries,
              l["Menu:Finance:Salaries"],
              url: "/Finance/Salaries",
              icon: "fas fa-money-bill-wave"
          ));

        // Günlük Raporlar
        financeMenu.AddItem(new ApplicationMenuItem(
            BeroxAppyMenus.FinanceReports,
            l["Menu:Finance:Reports"],
            url: "/Finance/Reports",
            icon: "fas fa-chart-bar"
        ));


        context.Menu.AddItem(financeMenu);

        if (MultiTenancyConsts.IsEnabled)
        {
            administration.SetSubItemOrder(TenantManagementMenuNames.GroupName, 1);
        }
        else
        {
            administration.TryRemoveMenuItem(TenantManagementMenuNames.GroupName);
        }

        administration.SetSubItemOrder(IdentityMenuNames.GroupName, 2);
        administration.SetSubItemOrder(SettingManagementMenuNames.GroupName, 3);

        return Task.CompletedTask;
    }
}
