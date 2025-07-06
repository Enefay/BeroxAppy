using Volo.Abp.Modularity;

namespace BeroxAppy;

public abstract class BeroxAppyApplicationTestBase<TStartupModule> : BeroxAppyTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
