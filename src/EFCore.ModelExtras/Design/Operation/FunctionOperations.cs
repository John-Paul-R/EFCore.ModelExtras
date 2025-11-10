using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EFCore.ModelExtras.Operations;

internal class AddFunctionOperation : PrettySqlOperation
{ }

internal class DropFunctionOperation : SqlOperation
{ }
