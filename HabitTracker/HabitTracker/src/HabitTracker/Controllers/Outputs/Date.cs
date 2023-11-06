using System.Text.Json.Serialization;

namespace HabitTracker.Controllers.Outputs
{
    public class Date
    {
        public int Year { get; init; }
        public int Month { get; init; }
        public int Day { get; init; }

        [JsonConstructor]
        public Date(int year, int month, int day)
        {
            Year = year;
            Month = month;
            Day = day;
        }

        public Date(DateTime dateTime)
        {
            Year = dateTime.Year;
            Month = dateTime.Month;
            Day = dateTime.Day;
        }
    }
}
