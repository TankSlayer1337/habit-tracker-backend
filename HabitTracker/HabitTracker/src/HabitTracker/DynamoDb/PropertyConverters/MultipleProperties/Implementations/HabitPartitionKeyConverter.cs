using HabitTracker.DynamoDb.Models;

namespace HabitTracker.DynamoDb.PropertyConverters.MultipleProperties.Implementations
{
    public class HabitPartitionKeyConverter : MultiplePropertiesConverter<HabitPartitionKey>
    {
        public override PropertyDefinition[] PropertyDefinitions => new PropertyDefinition[]
        {
            new PropertyDefinition
            {
                Name = PropertyConverterConstants.UserId,
                RegExPattern = PropertyConverterConstants.GuidPattern
            },
            new PropertyDefinition
            {
                Name = PropertyConverterConstants.Type,
                RegExPattern = PropertyConverterConstants.AlphabeticPattern
            }
        };

        public override HabitPartitionKey ToModel(List<string> orderedValues)
        {
            return new HabitPartitionKey
            {
                UserId = orderedValues[0],
                ItemType = orderedValues[1]
            };
        }

        public override List<string> ToOrderedValues(HabitPartitionKey model)
        {
            return new List<string> { model.UserId, model.ItemType };
        }
    }
}
