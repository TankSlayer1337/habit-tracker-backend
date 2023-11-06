using HabitTracker.DynamoDb.Models;

namespace HabitTracker.DynamoDb.PropertyConverters.MultipleProperties.Implementations
{
    public class HabitMonthRecordPointerConverter : MultiplePropertiesConverter<HabitMonthRecordPointer>
    {
        public override PropertyDefinition[] PropertyDefinitions => new PropertyDefinition[]
        {
            new PropertyDefinition
            {
                Name = PropertyConverterConstants.HabitId,
                RegExPattern = PropertyConverterConstants.GuidPattern
            },
            new PropertyDefinition
            {
                Name = PropertyConverterConstants.Year,
                RegExPattern = PropertyConverterConstants.YearPattern
            },
            new PropertyDefinition
            {
                Name = PropertyConverterConstants.Month,
                RegExPattern = PropertyConverterConstants.MonthPattern
            }
        };

        public override HabitMonthRecordPointer ToModel(List<string> orderedValues)
        {
            return new HabitMonthRecordPointer(orderedValues[0], int.Parse(orderedValues[1]), int.Parse(orderedValues[2]));
        }

        public override List<string> ToOrderedValues(HabitMonthRecordPointer model)
        {
            var year = model.Year.ToString() ?? string.Empty;
            var month = model.Month.ToString() ?? string.Empty;
            return new List<string> { model.HabitId, year, month };
        }
    }
}
