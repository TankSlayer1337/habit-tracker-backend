namespace HabitTracker.Controllers.Requests
{
    public class CreateHabitRequest
    {
        public string Name { get; init; }

        public CreateHabitRequest(string name)
        {
            Name = name;
        }
    }
}
