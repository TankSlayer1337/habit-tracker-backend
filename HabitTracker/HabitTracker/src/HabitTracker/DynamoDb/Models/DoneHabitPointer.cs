namespace HabitTracker.DynamoDb.Models
{
    public class DoneHabitPointer
    {
        public string HabitId { get; init; } = string.Empty;
        public string Date { get; init; } = string.Empty;
    }
}
