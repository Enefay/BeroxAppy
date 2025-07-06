using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace BeroxAppy.Data;

/* This is used if database provider does't define
 * IBeroxAppyDbSchemaMigrator implementation.
 */
public class NullBeroxAppyDbSchemaMigrator : IBeroxAppyDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
