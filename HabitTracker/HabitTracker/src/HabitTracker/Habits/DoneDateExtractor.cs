using HabitTracker.Controllers.Outputs;
using HabitTracker.DynamoDb.Models;

namespace HabitTracker.Habits
{
    public static class DoneDateExtractor
    {
        public static List<Date> GetDoneDatesInRange(List<HabitMonthRecordEntry> habitMonthRecordEntries, DateTime start, DateTime end)
        {
            var doneDates = new List<Date>();
            foreach (var entry in habitMonthRecordEntries)
            {
                (var year, var month) = entry.GetYearMonth();
                var entryStart = new DateTime(year, month, 1);
                var entryEnd = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                if (start <= entryEnd || end >= entryStart)
                {
                    foreach (var date in entry.Dates)
                    {
                        var dateTime = new DateTime(year, month, date);
                        if (start <= dateTime && dateTime <= end)
                        {
                            doneDates.Add(new Date(dateTime));
                        }
                    }
                }
            }
            return [.. doneDates.OrderBy(date => date.Year).ThenBy(date => date.Month).ThenBy(date => date.Day)];
        }
    }
}
