namespace HabitTracker.Controllers.Requests
{
    public class UpdateHabitRequest
    {
        public string HabitId { get; init; } = string.Empty;
        public string Name { get;init; } = string.Empty;
    }
}
