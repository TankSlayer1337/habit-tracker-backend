namespace HabitTracker.Habits.Extensions
{
    public static class DateTimeExtensions
    {
        public static bool IsTheDayBefore(this DateTime a, DateTime b)
        {
            return a.Date == b.AddDays(-1).Date;
        }

        public static string ToChartString(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy/MM/dd");
        }
    }
}
