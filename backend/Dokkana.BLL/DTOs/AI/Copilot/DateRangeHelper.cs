namespace Dokkana.BLL.AI.Copilot;

public static class DateRangeHelper
{
    public static (DateTime From, DateTime To) Resolve(string? period)
    {
        var now = DateTime.UtcNow;

        period = period?.Trim().ToLowerInvariant();

        return period switch
        {
            "today" => (
                now.Date,
                now.Date.AddDays(1)
            ),

            "yesterday" => (
                now.Date.AddDays(-1),
                now.Date
            ),

            "this_week" => GetThisWeekRange(now),

            "last_week" => GetLastWeekRange(now),

            "this_month" => (
                new DateTime(now.Year, now.Month, 1),
                new DateTime(now.Year, now.Month, 1).AddMonths(1)
            ),

            "last_month" => (
                new DateTime(now.Year, now.Month, 1).AddMonths(-1),
                new DateTime(now.Year, now.Month, 1)
            ),

            "this_year" => (
                new DateTime(now.Year, 1, 1),
                new DateTime(now.Year + 1, 1, 1)
            ),

            "all_time" => (
                DateTime.MinValue,
                DateTime.MaxValue
            ),

            _ => (
                new DateTime(now.Year, now.Month, 1),
                new DateTime(now.Year, now.Month, 1).AddMonths(1)
            )
        };
    }

    private static (DateTime From, DateTime To) GetThisWeekRange(DateTime now)
    {
        var diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
        var monday = now.Date.AddDays(-diff);

        return (monday, monday.AddDays(7));
    }

    private static (DateTime From, DateTime To) GetLastWeekRange(DateTime now)
    {
        var thisWeek = GetThisWeekRange(now);
        return (thisWeek.From.AddDays(-7), thisWeek.From);
    }
}