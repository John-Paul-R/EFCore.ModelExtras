using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EFCore.ModelExtras.Migrations;

internal interface IRelationalModelDiffer
{
    IEnumerable<MigrationOperation> GetOperations(
        IRelationalModel? source,
        IRelationalModel? target);
}
