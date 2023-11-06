using HabitTracker.Controllers.Requests;

namespace HabitTracker.DynamoDb.Models
{
    public class HabitMonthRecordPointer
    {
        public string HabitId { get; init; } = string.Empty;
        public int? Year { get; init; }
        public int? Month { get; init; }

        public HabitMonthRecordPointer(string habitId, int? year = null, int? month = null)
        {
            HabitId = habitId;
            Year = year;
            Month = month;
        }

        public static HabitMonthRecordPointer Create(DoneHabitRequest request)
        {
            return new HabitMonthRecordPointer(request.HabitId, request.Date.Year, request.Date.Month);
        }
    }
}
