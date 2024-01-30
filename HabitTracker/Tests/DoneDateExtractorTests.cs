using HabitTracker.Controllers.Outputs;
using HabitTracker.Habits;

namespace Tests
{
    public class DoneDateExtractorTests
    {
        [Fact]
        public void IncludeDoneDatesInRange()
        {
            // Arrange
            var year = 2024;
            var month = 1;
            var start = new DateTime(year, month, 1);
            var end = new DateTime(year, month, 31);
            var days = new int[] { 1, 4, 6, 8, 15, 25 };
            var recordDateTimes = days.Select(d => new DateTime(year, month, d)).ToArray();
            var records = TestData.CreateRecords(recordDateTimes);

            // Act
            var actualDates = DoneDateExtractor.GetDoneDatesInRange(records, start, end);

            // Assert
            var expectedDates = days.Select(d => new Date(year, month, d)).ToArray();
            AssertEqual(expectedDates, actualDates);
        }

        [Fact]
        public void ExcludeDatesOutsideRange()
        {
            // Arrange
            var year = 2024;
            var month = 6;
            var start = new DateTime(year, month, 1);
            var end = new DateTime(year, month, 30);
            var date1 = new DateTime(year, month - 1, 1);
            var date2 = new DateTime(year, month + 1, 1);
            var records = TestData.CreateRecords(date1, date2);

            // Act
            var actualDates = DoneDateExtractor.GetDoneDatesInRange(records, start, end);

            // Assert
            Assert.Empty(actualDates);
        }

        [Fact]
        public void IncludeDatesOnLimits()
        {
            // Arrange
            var year = 2024;
            var month = 1;
            var start = new DateTime(year, month, 1);
            var end = new DateTime(year, month, 31);
            var records = TestData.CreateRecords(start, end);

            // Act
            var actualDates = DoneDateExtractor.GetDoneDatesInRange(records, start, end);

            // Assert
            var expectedDates = new Date[] { new(start), new(end) };
            AssertEqual(expectedDates, actualDates);
        }

        [Fact]
        public void ReturnDatesInChronologicalOrder()
        {
            // Arrange
            var year = 2024;
            var start = new DateTime(year, 1, 1);
            var end = new DateTime(year, 2, 28);
            var shuffledDateTimes = new DateTime[] { new(year, 2, 25), new(year, 2, 10), new(year, 1, 10), new(year, 2, 26) };
            var records = TestData.CreateRecords(shuffledDateTimes);

            // Act
            var actualDates = DoneDateExtractor.GetDoneDatesInRange(records, start, end);

            // Assert
            var orderedDateTimes = shuffledDateTimes.Order();
            var expectedDates = orderedDateTimes.Select(d => new Date(year, d.Month, d.Day)).ToArray();
            AssertEqual(expectedDates, actualDates);
        }

        private static void AssertEqual(Date[] expectedDates, List<Date> actualDates)
        {
            for (var i = 0; i < expectedDates.Length; i++)
            {
                var expectedDate = expectedDates[i];
                var actualDate = actualDates[i];
                Assert.Equal(expectedDate.Year, actualDate.Year);
                Assert.Equal(expectedDate.Month, actualDate.Month);
                Assert.Equal(expectedDate.Day, actualDate.Day);
            }
        }
    }
}
