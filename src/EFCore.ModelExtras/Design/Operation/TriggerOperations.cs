using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EFCore.ModelExtras.Operations;

internal class AddTriggerOperation : PrettySqlOperation
{ }

internal class DropTriggerOperation : SqlOperation
{ }
