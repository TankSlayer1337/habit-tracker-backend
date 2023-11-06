using Amazon.DynamoDBv2.DataModel;
using HabitTracker.Controllers.Outputs;
using HabitTracker.DynamoDb.PropertyConverters;
using HabitTracker.DynamoDb.PropertyConverters.MultipleProperties.Implementations;

namespace HabitTracker.DynamoDb.Models
{
    public class HabitDefinitionEntry
    {
        [DynamoDBHashKey(AttributeNames.PK, typeof(HabitPartitionKeyConverter))]
        public HabitPartitionKey PartitionKey { get; init; } = new HabitPartitionKey();

        [DynamoDBRangeKey(AttributeNames.SK, typeof(HabitIdConverter))]
        public string HabitId { get; init; } = string.Empty;

        [DynamoDBProperty("Name")]
        public string Name { get; init; } = string.Empty;

        [DynamoDBIgnore]
        public static string ItemType { get; } = "HabitDefinition";

        public static HabitDefinitionEntry Create(string userId, string name)
        {
            return new HabitDefinitionEntry
            {
                PartitionKey = new HabitPartitionKey
                {
                    UserId = userId,
                    ItemType = ItemType
                },
                HabitId = Guid.NewGuid().ToString(),
                Name = name
            };
        }

        public HabitDefinitionEntry CloneWithNewName(string name)
        {
            return new HabitDefinitionEntry
            {
                PartitionKey = PartitionKey.Clone(),
                HabitId = HabitId,
                Name = name
            };
        }

        public HabitDefinition Convert()
        {
            return new HabitDefinition
            {
                HabitId = HabitId,
                Name = Name
            };
        }
    }
}
