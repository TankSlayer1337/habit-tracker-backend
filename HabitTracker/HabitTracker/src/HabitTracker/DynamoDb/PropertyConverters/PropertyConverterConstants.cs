namespace HabitTracker.DynamoDb.PropertyConverters
{
    public class PropertyConverterConstants
    {
        public const string UserId = "UserId";
        public const string Type = "Type";
        public const string HabitId = "HabitId";
        public const string Date = "Date";
        public const string Year = "Year";
        public const string Month = "Month";
        // when used for model validation the patterns should use ^ and $ anchors.
        public const string GuidPattern = @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}";
        public const string DatePattern = @"\d{4}-\d{2}-\d{2}";
        public const string AlphabeticPattern = @"[a-zA-Z]+";
        public const string YearPattern = @"\d{4}";
        public const string MonthPattern = @"(1[0-2]|[1-9])";
    }
}
