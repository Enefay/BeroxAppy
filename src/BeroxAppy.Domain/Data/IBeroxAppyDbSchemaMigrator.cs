using System.Threading.Tasks;

namespace BeroxAppy.Data;

public interface IBeroxAppyDbSchemaMigrator
{
    Task MigrateAsync();
}
