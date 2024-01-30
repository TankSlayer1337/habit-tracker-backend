using Amazon.DynamoDBv2.DataModel;
using HabitTracker.Controllers.Outputs;
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

        public bool ContainsDate(Date date)
        {
            return (date.Year == Pointer.Year && date.Month == Pointer.Month && Dates.Contains(date.Day));
        }

        /// <exception cref="NullReferenceException"></exception>
        public (int year, int month) GetYearMonth()
        {
            const string errorSuffix = " was null.";
            var year = Pointer.Year ?? throw new NullReferenceException("Year" + errorSuffix);
            var month = Pointer.Month ?? throw new NullReferenceException("Month" + errorSuffix);
            return (year, month);
        }
    }
}
