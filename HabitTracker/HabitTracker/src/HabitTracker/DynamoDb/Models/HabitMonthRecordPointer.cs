using HabitTracker.Controllers.Requests;

namespace HabitTracker.DynamoDb.Models
{
    public class HabitMonthRecordPointer(string habitId, int? year = null, int? month = null)
    {
        public string HabitId { get; init; } = habitId;
        // year and month nullable for querying purposes
        public int? Year { get; init; } = year;
        public int? Month { get; init; } = month;

        public static HabitMonthRecordPointer Create(DoneHabitRequest request)
        {
            return new HabitMonthRecordPointer(request.HabitId, request.Date.Year, request.Date.Month);
        }
    }
}
