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
