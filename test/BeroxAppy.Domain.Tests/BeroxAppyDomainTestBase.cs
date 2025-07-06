using Volo.Abp.Modularity;

namespace BeroxAppy;

/* Inherit from this class for your domain layer tests. */
public abstract class BeroxAppyDomainTestBase<TStartupModule> : BeroxAppyTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
