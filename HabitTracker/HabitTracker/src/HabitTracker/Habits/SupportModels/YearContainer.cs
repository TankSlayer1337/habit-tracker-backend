using HabitTracker.Controllers.Outputs;

namespace HabitTracker.Habits.SupportModels
{
    public class YearContainer
    {
        public readonly int Year;
        public readonly List<MonthContainer> Months = new();

        public YearContainer(Date date)
        {
            Year = date.Year;
            Months.Add(new MonthContainer(date.Month, date.Day));
        }

        public void AddIfNotIncluded(int month, int day)
        {
            if (TryGetMonthContainer(month, out var monthContainer))
            {
                monthContainer.AddIfNotIncluded(day);
            }
            else
            {
                Months.Add(new MonthContainer(month, day));
            }
        }

        public bool TryGetMonthContainer(int month, out MonthContainer monthContainer)
        {
            foreach (var container in Months)
            {
                if (container.Month == month)
                {
                    monthContainer = container;
                    return true;
                }
            }
            monthContainer = new MonthContainer(0, 0);
            return false;
        }
    }
}
