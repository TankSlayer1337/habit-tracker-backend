﻿using HabitTracker.DynamoDb.Models;
using HabitTracker.Habits;

namespace Tests
{
    public class ChartDataCreatorTests
    {
        private const string DateFormat = "yyyy-MM-dd";

        [Fact]
        public void CreateEmptyWhenNoRecords()
        {
            // Act
            var chartData = ChartDataCreator.Create([]);

            // Assert
            Assert.Empty(chartData.Dates);
            Assert.Empty(chartData.Values);
        }

        [Fact]
        public void CreateZeroPoint()
        {
            // Arrange
            var records = TestData.CreateRecords(DateTime.Now);

            // Act
            var chartData = ChartDataCreator.Create(records);

            // Assert
            Assert.Equal(2, chartData.Dates.Count);
            Assert.Equal(DateTime.Now.AddDays(-1).ToString(DateFormat), chartData.Dates[0]);
            Assert.Equal(0, chartData.Values[0]);
        }

        [Fact]
        public void CreateTodayPointIfMissing()
        {
            // Arrange
            var yesterday = DateTime.Now.AddDays(-1);
            var records = TestData.CreateRecords(yesterday);

            // Act
            var chartData = ChartDataCreator.Create(records);

            // Assert
            Assert.Equal(DateTime.Now.ToString(DateFormat), chartData.Dates.Last());
            Assert.Equal(1, chartData.Values.Last());
        }

        [Fact]
        public void AddPointBeforeDoneDateIfMissing()
        {
            // Arrange
            var doneDate1 = DateTime.Now.AddDays(-7);
            var doneDate2 = DateTime.Now;
            var records = TestData.CreateRecords(doneDate1, doneDate2);

            // Act
            var chartData = ChartDataCreator.Create(records);

            // Assert
            Assert.Equal(doneDate2.AddDays(-1).ToString(DateFormat), chartData.Dates[^2]);
            Assert.Equal(1, chartData.Values[^2]);
        }

        [Fact]
        public void CreateChartData()
        {
            // Arrange
            var faker = new Faker();
            var doneDatesCount = faker.Random.Int(1, 10);
            var doneDates = new DateTime[doneDatesCount];
            for (var i = 0; i < doneDatesCount; i++)
            {
                doneDates[i] = DateTime.Now.AddDays(-doneDatesCount + 1 + i);
            }
            var records = TestData.CreateRecords(doneDates);

            // Act
            var chartData = ChartDataCreator.Create(records);

            // Assert
            for (var i = 0; i < doneDatesCount; i++)
            {
                Assert.Equal(doneDatesCount - i, chartData.Values[^(1 + i)]);
                Assert.Equal(doneDates[^(1 + i)].ToString(DateFormat), chartData.Dates[^(1 + i)]);
            }
        }
    }
}
