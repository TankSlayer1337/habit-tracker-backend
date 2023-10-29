using Amazon.DynamoDBv2.DataModel;
using HabitTracker.DynamoDb.PropertyConverters;

namespace HabitTracker.DynamoDb.Models
{
    public class HabitDefinitionEntry
    {
        [DynamoDBHashKey(AttributeNames.PK, typeof(UserIdConverter))]
        public string UserId { get; init; } = string.Empty;

        [DynamoDBRangeKey(AttributeNames.SK, typeof(HabitIdConverter))]
        public string HabitId { get; init; } = string.Empty;

        [DynamoDBProperty("Name")]
        public string Name { get; init; } = string.Empty;

        public static HabitDefinitionEntry Create(string userId, string name)
        {
            return new HabitDefinitionEntry
            {
                UserId = userId,
                HabitId = Guid.NewGuid().ToString(),
                Name = name
            };
        }
    }
}
