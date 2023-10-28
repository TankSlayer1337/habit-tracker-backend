using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using System.Text.RegularExpressions;

namespace HabitTracker.DynamoDb.PropertyConverters
{
    public abstract class PrefixedGuidConverter : IPropertyConverter
    {
        protected abstract string _prefix { get; }

        public object FromEntry(DynamoDBEntry entry)
        {
            var data = (string)entry;
            if (string.IsNullOrEmpty(data))
            {
                throw new ArgumentOutOfRangeException(nameof(entry));
            }

            string pattern = $"^{_prefix}#(?<id>{PropertyConverterConstants.GuidPattern})$";
            var regex = new Regex(pattern);
            var match = regex.Match(data);

            if (match.Success)
            {
                return match.Groups["id"].Value;
            }

            throw new ArgumentOutOfRangeException(nameof(entry));
        }

        public DynamoDBEntry ToEntry(object value)
        {
            var data = (string)value;
            return $"{_prefix}#{data}";
        }
    }

    public class UserIdConverter : PrefixedGuidConverter
    {
        protected override string _prefix => PropertyConverterConstants.UserId;
    }

    public class HabitIdConverter : PrefixedGuidConverter
    {
        protected override string _prefix => PropertyConverterConstants.HabitId;
    }
}
