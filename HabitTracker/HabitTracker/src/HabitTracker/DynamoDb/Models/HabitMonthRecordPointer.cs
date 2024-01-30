namespace HabitTracker.DynamoDb.Models
{
    public class HabitMonthRecordPointer(string habitId, int? year = null, int? month = null)
    {
        public string HabitId { get; init; } = habitId;
        // year and month nullable for querying purposes
        public int? Year { get; init; } = year;
        public int? Month { get; init; } = month;
    }
}
