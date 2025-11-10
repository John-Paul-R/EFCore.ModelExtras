using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EFCoreUtility.ModelDiffer;

public interface IRelationalModelDiffer
{
    IEnumerable<MigrationOperation> GetOperations(
        IRelationalModel? source,
        IRelationalModel? target);
}
