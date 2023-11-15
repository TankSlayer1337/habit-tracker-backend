using HabitTracker.Controllers.Outputs;
using HabitTracker.DynamoDb.Models;

namespace HabitTracker.Habits.SupportModels
{
    public class TimeRecord
    {
        public List<YearContainer> Years { get; init; } = new List<YearContainer>();

        public static TimeRecord CreateWithDatesBetween(DateTime start, DateTime end)
        {
            var timeRecord = new TimeRecord();
            var dateRange = GetDatesBetween(start, end);
            dateRange.ForEach(date => timeRecord.AddIfNotIncluded(date));
            return timeRecord;
        }

        public List<Date> GetIntersectingDates(List<HabitMonthRecordEntry> habitMonthRecordEntries)
        {
            var intersectingDates = new List<Date>();
            foreach (var habitMonthRecordEntry in habitMonthRecordEntries)
            {
                var dates = GetIntersectingDates(habitMonthRecordEntry);
                intersectingDates.AddRange(dates);
            }
            return intersectingDates;
        }

        public List<Date> GetIntersectingDates(HabitMonthRecordEntry habitMonthRecordEntry)
        {
            const string errorMessage = " was null when trying to get intersecting dates.";
            var year = habitMonthRecordEntry.Pointer.Year ?? throw new Exception("Year" + errorMessage);
            var month = habitMonthRecordEntry.Pointer.Month ?? throw new Exception("Month" + errorMessage);
            var intersectingDates = new List<Date>();
            if (TryGetYearContainer(year, out var yearContainer) && yearContainer.TryGetMonthContainer(month, out var monthContainer))
            {
                var days = habitMonthRecordEntry.Dates.Intersect(monthContainer.Days).ToList();
                intersectingDates = days.Select(day => new Date(year, month, day)).ToList();
            }
            return intersectingDates;
        }

        private void AddIfNotIncluded(Date date)
        {
            if (TryGetYearContainer(date.Year, out var yearContainer))
            {
                yearContainer.AddIfNotIncluded(date.Month, date.Day);
            } else
            {
                Years.Add(new YearContainer(date));
            }
        }

        private bool TryGetYearContainer(int year, out YearContainer yearContainer)
        {
            foreach (var container in Years)
            {
                if (container.Year == year)
                {
                    yearContainer = container;
                    return true;
                }
            }
            yearContainer = new YearContainer(new Date(0, 0, 0));
            return false;
        }

        private static List<Date> GetDatesBetween(DateTime start, DateTime end)
        {
            var dates = new List<Date>();
            for (var date = start; date <= end; date = date.AddDays(1))
            {
                dates.Add(new Date(date));
            }
            return dates;
        }
    }
}
