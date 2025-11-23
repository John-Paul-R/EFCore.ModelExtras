using EFCore.ModelExtras.Core.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EFCore.ModelExtras.FunctionsAndTriggers.Operations;

internal class AddFunctionOperation : PrettySqlOperation
{ }

internal class DropFunctionOperation : SqlOperation
{ }
