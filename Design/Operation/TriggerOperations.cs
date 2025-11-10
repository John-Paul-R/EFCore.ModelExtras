using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Jp.Entities.Models.DbContext.Design.Operation;

public class AddTriggerOperation : PrettySqlOperation
{ }

public class DropTriggerOperation : SqlOperation
{ }
