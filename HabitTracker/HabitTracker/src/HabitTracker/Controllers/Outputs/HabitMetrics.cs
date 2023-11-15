namespace HabitTracker.Controllers.Outputs
{
    public class HabitMetrics : HabitRecord
    {
        public ChartData ChartData { get; init; }

        public HabitMetrics(HabitDefinition habitDefinition, int allTimeDoneDatesCount, Date startDate, Date endDate, List<Date> doneDates, ChartData chartData) : base(habitDefinition, allTimeDoneDatesCount, startDate, endDate, doneDates)
        {
            ChartData = chartData;
        }
    }
}
