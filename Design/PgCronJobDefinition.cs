using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Jp.Entities.Models.DbContext.Design;

// https://github.com/citusdata/pg_cron

/// <param name="Name">The name of the job</param>
/// <param name="Source">The job source code</param>
public record PgCronJobDefinition(
    string JobName,
    [StringSyntax("cron")] string CronExpression,
    [StringSyntax("sql")] string Source)
: SqlObjectDeclaration(JobName)
{
    public override string UniqueKey => Name;
}

public interface IBaseCronExpressions
{
    string EveryMonth { get; }
    string EverySixMonths { get; }
}

public static partial class CronExpressions
{
    public static IBaseCronExpressions Zeros { get; } = new ZeroesCronExpressions();

    public static IBaseCronExpressions Randomized(string consistentScrambleKey)
        => new RandomizedCronExpressions(consistentScrambleKey);

    private sealed class ZeroesCronExpressions : IBaseCronExpressions
    {
        public string EveryMonth { get; } = "0 0 1 * *";
        public string EverySixMonths { get; } = "0 0 1 */6 *";
    }


    private partial class RandomizedCronExpressions : IBaseCronExpressions
    {
        private Random _rnd;

        public RandomizedCronExpressions(string consistentScrambleKey)
        {
            _rnd = new Random((int)JpDbContextExtensions.FNV1a(consistentScrambleKey));
        }

        public string EveryMonth => ScrambleTimeOfDay(Zeros.EveryMonth);
        public string EverySixMonths => ScrambleTimeOfDay(Zeros.EverySixMonths);

        [GeneratedRegex(
            @"^(\S+)\s+"     +  // Group 1: minute (0-59)
            @"(\S+)\s+"      +  // Group 2: hour (0-23)
            @"(\S+)\s+"      +  // Group 3: day of month (1-31)
            @"(\S+)\s+"      +  // Group 4: month (1-12)
            @"(\S+)"         +  // Group 5: day of week (0-7)
            @"(?:\s+(\S+))?" +  // Group 6: year (optional)
            @"(?:\s+(\S+))?$"   // Group 7: seconds (optional)
        )]
        private static partial Regex CronComponentsRegex();

        private string ScrambleTimeOfDay(string cronExpression)
        {
            var match = CronComponentsRegex().Match(cronExpression.Trim());

            if (!match.Success) {
                throw new ArgumentException("Invalid cron expression format", nameof(cronExpression));
            }

            // var _minute =  match.Groups[1].Value;
            // var _hour =    match.Groups[2].Value;
            var day =     match.Groups[3].Value;
            var month =   match.Groups[4].Value;
            var weekday = match.Groups[5].Value;
            var year =    match.Groups[6].Success ? match.Groups[6].Value : null;
            var seconds = match.Groups[7].Success ? match.Groups[7].Value : null;

            // rnd.Next upper bound is exclusive, same as cron, so this gets to look simple
            var randomMinute = _rnd.Next(0, 60).ToString();
            var randomHour = _rnd.Next(0, 24).ToString();

            List<string> components = new(7);
            components.AddRange([randomMinute, randomHour, day, month, weekday]);

            if (!string.IsNullOrEmpty(year)) {
                components.Add(year);
            }
            if (!string.IsNullOrEmpty(seconds)) {
                components.Add(seconds);
            }

            return string.Join(" ", components);
        }
    }
}
