using Volo.Abp.Settings;

namespace BeroxAppy.Settings;

public class BeroxAppySettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(BeroxAppySettings.MySetting1));
    }
}
