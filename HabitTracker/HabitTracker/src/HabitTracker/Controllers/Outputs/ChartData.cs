namespace HabitTracker.Controllers.Outputs
{
    public class ChartData
    {
        public List<string> Dates { get; init; }
        public List<int> Values { get; init; }

        public ChartData(List<string> dates, List<int> values)
        {
            Dates = dates;
            Values = values;
        }

        public static ChartData CreateEmpty()
        {
            return new ChartData(new List<string>(), new List<int>());
        }
    }
}
