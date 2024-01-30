using HabitTracker.Habits.Extensions;

namespace HabitTracker.Controllers.Outputs
{
    public class ChartData
    {
        public List<string> Dates { get; } = [];
        public List<int> Values { get; } = [];

        public void Add(DateTime date, int value)
        {
            Dates.Add(date.ToChartString());
            Values.Add(value);
        }
    }
}
