namespace HabitTracker.Habits.SupportModels
{
    public class MonthContainer
    {
        public int Month { get; init; }
        public List<int> Days { get; init; } = new List<int>();

        public MonthContainer(int month, int day)
        {
            Month = month;
            Days.Add(day);
        }

        public void AddIfNotIncluded(int day)
        {
            if (Days.Contains(day))
                return;
            Days.Add(day);
        }
    }
}
