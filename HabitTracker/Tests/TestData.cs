using HabitTracker.DynamoDb.Models;

namespace Tests
{
    internal class TestData
    {
        public static List<HabitMonthRecordEntry> CreateRecords(params DateTime[] dates)
        {
            var habitId = Guid.NewGuid().ToString();
            var records = new List<HabitMonthRecordEntry>();
            foreach (var date in dates)
            {
                var existingRecord = records.SingleOrDefault(r =>
                {
                    var (year, month) = r.GetYearMonth();
                    return year == date.Year && month == date.Month;
                });
                if (existingRecord == null)
                {
                    records.Add(new HabitMonthRecordEntry
                    {
                        Pointer = new(habitId, date.Year, date.Month),
                        Dates = [date.Day]
                    });
                    continue;
                }
                existingRecord.Dates.Add(date.Day);
            }
            return records;
        }
    }
}
