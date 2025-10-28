using Novibet.Domain.Entities;

namespace Novibet.Infrastructure.Helpers;

public static class BuildSql
{
    public static string Merge(IEnumerable<CurrencyRate> rates)
    {
        var values = string.Join(",", rates.Select(r =>
            $"('{r.Currency}', {r.Rate}, '{r.Date:yyyy-MM-dd}')"));

        return $@"
        MERGE INTO ""CurrencyRates"" AS target
        USING (VALUES {values}) AS source(""Currency"", ""Rate"", ""Date"")
        ON target.""Currency"" = source.""Currency"" AND target.""Date"" = source.""Date""
        WHEN MATCHED THEN UPDATE SET ""Rate"" = source.""Rate""
        WHEN NOT MATCHED THEN INSERT (""Currency"", ""Rate"", ""Date"") VALUES (source.""Currency"", source.""Rate"", source.""Date"");";
    }
}