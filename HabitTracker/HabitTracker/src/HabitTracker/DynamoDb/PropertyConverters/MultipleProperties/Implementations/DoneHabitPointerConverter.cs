using HabitTracker.DynamoDb.Models;

namespace HabitTracker.DynamoDb.PropertyConverters.MultipleProperties.Implementations
{
    public class DoneHabitPointerConverter : MultiplePropertiesConverter<DoneHabitPointer>
    {
        public override PropertyDefinition[] PropertyDefinitions => new PropertyDefinition[]
        {
            new PropertyDefinition
            {
                Name = PropertyConverterConstants.Date,
                RegExPattern = PropertyConverterConstants.DatePattern
            },
            new PropertyDefinition
            {
                Name = PropertyConverterConstants.HabitId,
                RegExPattern= PropertyConverterConstants.GuidPattern
            }
        };

        public override DoneHabitPointer ToModel(List<string> orderedValues)
        {
            return new DoneHabitPointer
            {
                Date = orderedValues[0],
                HabitId = orderedValues[1]
            };
        }

        public override List<string> ToOrderedValues(DoneHabitPointer model)
        {
            return new List<string> { model.Date, model.HabitId };
        }
    }
}
