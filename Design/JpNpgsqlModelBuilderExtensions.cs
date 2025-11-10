using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Npgsql.NameTranslation;
using NpgsqlTypes;

namespace Jp.Entities.Models.DbContext.Design;

/// <remarks>
/// This class copies patterns from <see cref="NpgsqlModelBuilderExtensions"/>.
///
/// see also: https://www.npgsql.org/efcore/api/Microsoft.EntityFrameworkCore.NpgsqlModelBuilderExtensions.html
/// </remarks>
public static class JpNpgsqlModelBuilderExtensions
{
    /// <summary>
    ///     Registers a user-defined enum type in the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder in which to create the enum type.</param>
    /// <param name="schema">The schema in which to create the enum type.</param>
    /// <param name="name">The name of the enum type to create.</param>
    /// <param name="nameTranslator">
    ///     The translator for name and label inference.
    ///     Defaults to <see cref="NpgsqlSnakeCaseNameTranslator" />.
    /// </param>
    /// <typeparam name="TEnum"></typeparam>
    /// <returns>
    ///     The updated <see cref="ModelBuilder" />.
    /// </returns>
    /// <remarks>
    ///     See: https://www.postgresql.org/docs/current/static/datatype-enum.html
    /// </remarks>
    /// <exception cref="ArgumentNullException">builder</exception>
    public static ModelBuilder HasPostgresEnum<TEnum>(
        this ModelBuilder modelBuilder,
        string? schema = null,
        string? name = null,
        INpgsqlNameTranslator? nameTranslator = null,
        Func<FieldInfo, bool>? fieldWhere = null)
    where TEnum : struct, Enum
    {
        // This should work with any configuration, so we allow the obsolete
        // tech as a fallback if you didn't explicitly configure.
        // This pulls from the NpgsqlModelBuilderExtensions, as previously stated.
#pragma warning disable CS0618 // NpgsqlConnection.GlobalTypeMapper is obsolete
        nameTranslator ??= NpgsqlConnection.GlobalTypeMapper.DefaultNameTranslator;
#pragma warning restore CS0618

        return modelBuilder.HasPostgresEnum(
            schema,
            name ?? GetTypePgName(typeof(TEnum), nameTranslator),
            GetMemberPgNames(typeof(TEnum), nameTranslator, fieldWhere ?? ((_) => true)));
    }

    public static ModelBuilder HasPostgresEnum(
        this ModelBuilder modelBuilder,
        Type enumType,
        string? schema = null,
        string? name = null,
        INpgsqlNameTranslator? nameTranslator = null,
        Func<FieldInfo, bool>? fieldWhere = null)
    {
        if (!enumType.IsEnum) {
            throw new InvalidOperationException($"'{enumType.Name}' is not an enum!");
        }
        // This should work with any configuration, so we allow the obsolete
        // tech as a fallback if you didn't explicitly configure.
        // This pulls from the NpgsqlModelBuilderExtensions, as previously stated.
#pragma warning disable CS0618 // NpgsqlConnection.GlobalTypeMapper is obsolete
        nameTranslator ??= NpgsqlConnection.GlobalTypeMapper.DefaultNameTranslator;
#pragma warning restore CS0618

        return modelBuilder.HasPostgresEnum(
            schema,
            name ?? GetTypePgName(enumType, nameTranslator),
            GetMemberPgNames(enumType, nameTranslator, fieldWhere ?? ((_) => true)));
    }

    // Dead link courtesy of the npgsql folks
    // See: https://github.com/npgsql/npgsql/blob/dev/src/Npgsql/TypeMapping/TypeMapperBase.cs#L132-L138
    private static string GetTypePgName(Type enumType, INpgsqlNameTranslator nameTranslator)
        => enumType.GetCustomAttribute<PgNameAttribute>()?.PgName ?? nameTranslator.TranslateTypeName(enumType.Name);

    // Dead link courtesy of the npgsql folks
    // See: https://github.com/npgsql/npgsql/blob/dev/src/Npgsql/TypeHandlers/EnumHandler.cs#L118-L129
    private static string[] GetMemberPgNames(Type enumType, INpgsqlNameTranslator nameTranslator, Func<FieldInfo, bool> fieldWhere)
        => enumType
            .GetFields(BindingFlags.Static | BindingFlags.Public)
            .Where(fieldWhere)
            .Select(x => x.GetCustomAttribute<PgNameAttribute>()?.PgName ?? nameTranslator.TranslateMemberName(x.Name))
            .ToArray();
}
