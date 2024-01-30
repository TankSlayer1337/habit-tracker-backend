using HabitTracker.Controllers.Outputs;
using HabitTracker.DynamoDb.Models;
using HabitTracker.Habits.Extensions;

namespace HabitTracker.Habits
{
    public static class ChartDataCreator
    {
        public static ChartData Create(List<HabitMonthRecordEntry> habitRecords)
        {
            var chartData = new ChartData();
            if (habitRecords.Count == 0)
                return chartData;
            var dates = GetDatesInChronologicalOrder(habitRecords);
            var dayBeforeFirstRecordedHabit = dates[0].AddDays(-1);
            dates.Insert(0, dayBeforeFirstRecordedHabit);
            chartData.Add(dates[0], 0);
            var doneCount = 0;
            for (var i = 1; i < dates.Count; i++)
            {
                if (!dates[i - 1].IsTheDayBefore(dates[i]))
                {
                    var previousDate = dates[i].AddDays(-1);
                    chartData.Add(previousDate, doneCount);
                }
                doneCount++;
                chartData.Add(dates[i], doneCount);
            }
            if (dates.Last().Date != DateTime.Today)
            {
                chartData.Add(DateTime.Today, doneCount);
            }
            return chartData;
        }

        private static List<DateTime> GetDatesInChronologicalOrder(List<HabitMonthRecordEntry> habitMonthRecords)
        {
            var dates = new List<DateTime>();
            foreach (var habitMonthRecord in habitMonthRecords)
            {
                (var year, var month) = habitMonthRecord.GetYearMonth();
                var recordedDates = habitMonthRecord.Dates.Select(date => new DateTime(year, month, date));
                dates.AddRange(recordedDates);
            }
            var orderedDates = dates.OrderBy(date => date.Year).ThenBy(date => date.Month).ThenBy(date => date.Day).ToList();
            return orderedDates;
        }
    }
}
