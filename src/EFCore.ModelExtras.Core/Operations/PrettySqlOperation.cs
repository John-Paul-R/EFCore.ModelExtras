using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EFCore.ModelExtras.Core.Operations;

/// <summary>
/// A marker operation that signals to C# migration generators that this SQL should be
/// formatted with pretty syntax (e.g., raw string literals with /*lang=sql*/ comments).
/// </summary>
public class PrettySqlOperation : SqlOperation
{

}
