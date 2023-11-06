namespace HabitTracker.Controllers.Outputs
{
    public class HabitRecord
    {
        public string HabitId { get; init; }
        public string Name { get; init; }
        public int AllTimeDoneDatesCount { get; init; }
        public Date StartDate { get; init; }
        public Date EndDate { get; init; }
        public List<Date> DoneDates { get; init; }

        public HabitRecord(HabitDefinition habitDefinition, int allTimeDoneDatesCount, Date startDate, Date endDate, List<Date> doneDates)
        {
            HabitId = habitDefinition.HabitId;
            Name = habitDefinition.Name;
            AllTimeDoneDatesCount = allTimeDoneDatesCount;
            StartDate = startDate;
            EndDate = endDate;
            DoneDates = doneDates;
        }
    }
}
