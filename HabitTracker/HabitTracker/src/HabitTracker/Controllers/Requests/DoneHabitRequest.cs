using HabitTracker.DynamoDb.PropertyConverters;
using System.ComponentModel.DataAnnotations;

namespace HabitTracker.Controllers.Requests
{
    public class DoneHabitRequest
    {
        public string HabitId { get; init; } = string.Empty;
        [RegularExpression(PropertyConverterConstants.DatePattern)]
        public string Date { get; init; } = string.Empty;
    }
}
