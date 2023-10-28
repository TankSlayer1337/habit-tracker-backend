using Amazon.DynamoDBv2.DataModel;
using HabitTracker.DynamoDb.PropertyConverters;
using HabitTracker.DynamoDb.PropertyConverters.MultipleProperties.Implementations;

namespace HabitTracker.DynamoDb.Models
{
    public class DoneHabitEntry
    {
        [DynamoDBHashKey(AttributeNames.PK, typeof(UserIdConverter))]
        public string UserId { get; init; } = string.Empty;

        [DynamoDBRangeKey(AttributeNames.SK, typeof(DoneHabitPointerConverter))]
        public DoneHabitPointer DoneHabitPointer { get; init; } = new DoneHabitPointer();
    }
}
