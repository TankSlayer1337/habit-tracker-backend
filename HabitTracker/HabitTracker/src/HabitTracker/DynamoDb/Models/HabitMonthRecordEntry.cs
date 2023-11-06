using Amazon.DynamoDBv2.DataModel;
using HabitTracker.DynamoDb.PropertyConverters.MultipleProperties.Implementations;

namespace HabitTracker.DynamoDb.Models
{
    public class HabitMonthRecordEntry
    {
        [DynamoDBHashKey(AttributeNames.PK, typeof(HabitPartitionKeyConverter))]
        public HabitPartitionKey PartitionKey { get; init; } = new HabitPartitionKey();

        [DynamoDBRangeKey(AttributeNames.SK, typeof(HabitMonthRecordPointerConverter))]
        public HabitMonthRecordPointer Pointer { get; init; }

        [DynamoDBProperty("Dates")]
        public List<int> Dates { get; init; } = new List<int>();

        [DynamoDBVersion]
        public int? VersionNumber { get; set; }

        public static HabitPartitionKey CreatePartitionKey(string userId)
        {
            return new HabitPartitionKey()
            {
                UserId = userId,
                ItemType = "HabitMonthRecordEntry"
            };
        }
    }
}
