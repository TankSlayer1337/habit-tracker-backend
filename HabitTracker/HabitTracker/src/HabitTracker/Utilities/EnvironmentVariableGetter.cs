namespace HabitTracker.Utilities
{
    public class EnvironmentVariableGetter
    {
        public string Get(string name)
        {
            return Environment.GetEnvironmentVariable(name) ?? throw new Exception($"Missing environment variable {name}");
        }
    }
}
