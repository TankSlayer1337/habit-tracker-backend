using HabitTracker.Controllers.Outputs;

namespace HabitTracker.Controllers.Requests
{
    public class DoneHabitRequest
    {
        public string HabitId { get; init; } = string.Empty;
        public Date Date { get; init; } = new Date(DateTime.Now);
    }
}
