namespace HabitTracker.Controllers.Outputs
{
    public class HabitRecord
    {
        public string HabitId { get; init; } = string.Empty;
        public string Name {  get; init; } = string.Empty;
        public List<string> Dates { get; init; } = new List<string>();
    }
}
