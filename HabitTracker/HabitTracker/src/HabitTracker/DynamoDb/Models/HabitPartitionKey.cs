namespace HabitTracker.DynamoDb.Models
{
    public class HabitPartitionKey
    {
        public string UserId { get; init; } = string.Empty;
        public string ItemType { get; init; } = string.Empty;

        public HabitPartitionKey Clone()
        {
            return new HabitPartitionKey
            {
                UserId = UserId,
                ItemType = ItemType
            };
        }
    }
}
