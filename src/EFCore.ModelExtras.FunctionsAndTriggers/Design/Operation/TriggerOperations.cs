using EFCore.ModelExtras.Core.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EFCore.ModelExtras.FunctionsAndTriggers.Operations;

internal class AddTriggerOperation : PrettySqlOperation
{ }

internal class DropTriggerOperation : SqlOperation
{ }
