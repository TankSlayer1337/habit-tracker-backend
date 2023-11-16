using Amazon.DynamoDBv2.DocumentModel;
using HabitTracker.Controllers.Outputs;
using HabitTracker.Controllers.Requests;
using HabitTracker.DynamoDb;
using HabitTracker.DynamoDb.Models;
using HabitTracker.Habits.Extensions;
using HabitTracker.UserInfo;

namespace HabitTracker.Habits
{
    public class HabitRepository
    {
        private readonly UserInfoGetter _userInfoGetter;
        private readonly DynamoDbContextWrapper _dynamoDbContext;

        public HabitRepository(UserInfoGetter userInfoGetter, DynamoDbContextWrapper dynamoDbContext)
        {
            _userInfoGetter = userInfoGetter;
            _dynamoDbContext = dynamoDbContext;
        }

        public async Task CreateHabit(string authorizationHeader, string habitName)
        {
            var userId = await _userInfoGetter.GetUserIdAsync(authorizationHeader);
            var habitDefinitionEntry = HabitDefinitionEntry.Create(userId, habitName);
            await _dynamoDbContext.SaveAsync(habitDefinitionEntry);
        }

        public async Task UpdateHabit(string authorizationHeader, UpdateHabitRequest request)
        {
            var userId = await _userInfoGetter.GetUserIdAsync(authorizationHeader);
            var habitDefinitionEntry = await GetHabitDefinitionAsync(userId, request.HabitId);
            var updatedEntry = habitDefinitionEntry.CloneWithNewName(request.Name);
            await _dynamoDbContext.SaveAsync(updatedEntry);
        }

        public async Task DeleteHabit(string authorizationHeader, string habitId)
        {
            var userId = await _userInfoGetter.GetUserIdAsync(authorizationHeader);
            var habitDefinitionEntry = await GetHabitDefinitionAsync(userId, habitId);
            var doneHabitEntries = await GetAllHabitMonthRecordEntriesAsync(userId, habitId);
            foreach (var entry in doneHabitEntries)
            {
                await _dynamoDbContext.DeleteAsync(entry);
            }
            await _dynamoDbContext.DeleteAsync(habitDefinitionEntry);
        }

        private async Task<List<HabitDefinition>> GetHabitDefinitionsByUserId(string userId)
        {
            var partitionKey = new HabitPartitionKey
            {
                UserId = userId,
                ItemType = HabitDefinitionEntry.ItemType
            };
            var habitDefinitionEntries = await _dynamoDbContext.QueryWithEmptyBeginsWithAsync<HabitDefinitionEntry>(partitionKey);
            var habitDefinitions = habitDefinitionEntries.Select(entry => entry.Convert()).ToList();
            return habitDefinitions;
        }

        public async Task RegisterDoneHabit(string authorizationHeader, DoneHabitRequest request)
        {
            var userId = await _userInfoGetter.GetUserIdAsync(authorizationHeader);
            await VerifyHabitExists(userId, request.HabitId);
            var date = request.Date;
            var pointer = new HabitMonthRecordPointer(request.HabitId, date.Year, date.Month);
            var habitMonthRecordEntries = await GetHabitMonthRecordEntries(userId, pointer);
            if (habitMonthRecordEntries == null || !habitMonthRecordEntries.Any())
            {
                var newEntry = CreateHabitMonthRecordEntry(userId, request);
                await _dynamoDbContext.SaveAsync(newEntry);
                return;
            }
            var entry = habitMonthRecordEntries.Single();
            await AddDayToRecord(entry, date.Day);
        }

        private async Task AddDayToRecord(HabitMonthRecordEntry entry, int day)
        {
            for (var i = 0; i < 10; i++)
            {
                if (entry.Dates.Contains(day))
                    return;
                entry.Dates.Add(day);
                try
                {
                    await _dynamoDbContext.SaveAsync(entry);
                    return;
                } catch
                {
                    var latestEntries = await _dynamoDbContext.QueryAsync<HabitMonthRecordEntry>(entry.PartitionKey, QueryOperator.Equal, new HabitMonthRecordPointer[] { entry.Pointer });
                    if (latestEntries == null || !latestEntries.Any())
                    {
                        entry.VersionNumber = null;
                        continue;
                    }
                    entry = latestEntries.Single();
                }
            }
            throw new Exception($"Failed to add done date {entry.Pointer.Year}-{entry.Pointer.Month}-{day} to Habit with ID ${entry.Pointer.HabitId}.");
        }

        private async Task RemoveDayFromRecord(HabitMonthRecordEntry entry, int day)
        {
            for (var i = 0; i < 10; i++)
            {
                if (!entry.Dates.Contains(day))
                    return;
                entry.Dates.Remove(day);
                try
                {
                    if (entry.Dates.Any())
                    {
                        await _dynamoDbContext.SaveAsync(entry);
                    }
                    else
                    {
                        await _dynamoDbContext.DeleteAsync(entry);
                    }
                }
                catch
                {
                    var latestEntries = await _dynamoDbContext.QueryAsync<HabitMonthRecordEntry>(entry.PartitionKey, QueryOperator.Equal, new HabitMonthRecordPointer[] { entry.Pointer });
                    if (latestEntries == null || !latestEntries.Any())
                        return;
                    entry = latestEntries.Single();
                }
            }
            throw new Exception($"Failed to remove done date {entry.Pointer.Year}-{entry.Pointer.Month}-{day} from Habit with ID ${entry.Pointer.HabitId}.");
        }

        private static HabitMonthRecordEntry CreateHabitMonthRecordEntry(string userId, DoneHabitRequest request)
        {
            return new HabitMonthRecordEntry
            {
                PartitionKey = HabitMonthRecordEntry.CreatePartitionKey(userId),
                Pointer = new HabitMonthRecordPointer(request.HabitId, request.Date.Year, request.Date.Month),
                Dates = new List<int> { request.Date.Day }
            };
        }

        public async Task DeleteDoneHabit(string authorizationHeader, DoneHabitRequest request)
        {
            var userId = await _userInfoGetter.GetUserIdAsync(authorizationHeader);
            var date = request.Date;
            var pointer = HabitMonthRecordPointer.Create(request);
            var habitMonthRecordEntries = await GetHabitMonthRecordEntries(userId, pointer);
            if (habitMonthRecordEntries == null || !habitMonthRecordEntries.Any())
            {
                throw new BadHttpRequestException($"Record of done habit with ID {request.HabitId} and Date {date.Year}-{date.Month}-{date.Day} was not found in the database.");
            }
            var entry = habitMonthRecordEntries.Single();
            await RemoveDayFromRecord(entry, date.Day);
        }

        private async Task<List<HabitMonthRecordEntry>> GetHabitMonthRecordEntries(string userId, HabitMonthRecordPointer pointer)
        {
            var partitionKey = HabitMonthRecordEntry.CreatePartitionKey(userId);
            var sortKeyValues = new HabitMonthRecordPointer[] { pointer };
            return await _dynamoDbContext.QueryAsync<HabitMonthRecordEntry>(partitionKey, QueryOperator.Equal, sortKeyValues);
        }

        private static ChartData GetChartData(List<HabitMonthRecordEntry> habitRecords)
        {
            var dates = GetDatesInChronologicalOrder(habitRecords);
            var chartData = new ChartData();
            var dayBeforeFirstRecordedHabit = dates[0].AddDays(-1);
            dates.Insert(0, dayBeforeFirstRecordedHabit);
            chartData.Add(dates[0], 0);
            var doneCount = 0;
            for (var i = 1; i < dates.Count; i++)
            {
                if (!dates[i - 1].IsTheDayBefore(dates[i]))
                {
                    var previousDate = dates[i].AddDays(-1);
                    chartData.Add(previousDate, doneCount);
                }
                doneCount++;
                chartData.Add(dates[i], doneCount);
            }
            if (dates.Last().Date != DateTime.Today)
            {
                chartData.Add(DateTime.Today, doneCount);
            }
            return chartData;
        }

        private static List<DateTime> GetDatesInChronologicalOrder(List<HabitMonthRecordEntry> habitMonthRecords)
        {
            var dates = new List<DateTime>();
            foreach (var habitMonthRecord in habitMonthRecords)
            {
                (var year, var month) = GetYearMonth(habitMonthRecord);
                var recordedDates = habitMonthRecord.Dates.Select(date => new DateTime(year, month, date));
                dates.AddRange(recordedDates);
            }
            var orderedDates = dates.OrderBy(date => date.Year).ThenBy(date => date.Month).ThenBy(date => date.Day).ToList();
            return orderedDates;
        }

        private static (int year, int month) GetYearMonth(HabitMonthRecordEntry habitMonthRecord)
        {
            const string errorSuffix = " was null.";
            var year = habitMonthRecord.Pointer.Year ?? throw new Exception("Year" + errorSuffix);
            var month = habitMonthRecord.Pointer.Month ?? throw new Exception("Month" + errorSuffix);
            return (year, month);

        }

        public async Task<List<HabitRecord>> GetHabitRecordsForPastWeek(string authorizationHeader)
        {
            var userId = await _userInfoGetter.GetUserIdAsync(authorizationHeader);
            var end = DateTime.Now.Date;
            var start = end.AddDays(-6).Date;
            return await GetHabitRecordsForPeriod(userId, start, end);
        }

        private async Task<List<HabitRecord>> GetHabitRecordsForPeriod(string userId, DateTime start, DateTime end)
        {
            var habitDefinitions = await GetHabitDefinitionsByUserId(userId);
            var habitRecords = new List<HabitRecord>();
            foreach (var definition in habitDefinitions)
            {
                var habitMonthRecordEntries = await GetAllHabitMonthRecordEntriesAsync(userId, definition.HabitId);
                var allTimeDoneDatesCount = habitMonthRecordEntries.Select(entry => entry.Dates.Count).Sum();
                var doneDates = GetDoneDatesInRange(habitMonthRecordEntries, start, end);
                var chartData = GetChartData(habitMonthRecordEntries);
                habitRecords.Add(new HabitRecord(definition, allTimeDoneDatesCount, new Date(start), new Date(end), doneDates, chartData));
            }
            return habitRecords;
        }

        private static List<Date> GetDoneDatesInRange(List<HabitMonthRecordEntry> habitMonthRecordEntries, DateTime start, DateTime end)
        {
            var doneDates = new List<Date>();
            foreach (var entry in habitMonthRecordEntries)
            {
                (var year, var month) = GetYearMonth(entry);
                var entryStart = new DateTime(year, month, 1);
                var entryEnd = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                if (start <= entryEnd || end >= entryStart)
                {
                    var datesInRange = entry.Dates.Where(date => start.Day <= date && date <= end.Day).Select(date => new Date(year, month, date)).ToList();
                    doneDates.AddRange(datesInRange);
                }
            }
            return doneDates.OrderBy(date => date.Year).ThenBy(date => date.Month).ThenBy(date => date.Day).ToList();
        }

        private async Task<List<HabitMonthRecordEntry>> GetAllHabitMonthRecordEntriesAsync(string userId, string habitId)
        {
            var partitionKey = HabitMonthRecordEntry.CreatePartitionKey(userId);
            var sortKeyValues = new HabitMonthRecordPointer[] { new HabitMonthRecordPointer(habitId) };
            var habitMonthRecordEntries = await _dynamoDbContext.QueryAsync<HabitMonthRecordEntry>(partitionKey, QueryOperator.BeginsWith, sortKeyValues);
            return habitMonthRecordEntries;
        }

        private async Task VerifyHabitExists(string userId, string habitId)
        {
            await GetHabitDefinitionAsync(userId, habitId);
        }

        private async Task<HabitDefinitionEntry> GetHabitDefinitionAsync(string userId, string habitId)
        {
            var partitionKey = new HabitPartitionKey
            {
                UserId = userId,
                ItemType = HabitDefinitionEntry.ItemType
            };
            var habits = await _dynamoDbContext.QueryAsync<HabitDefinitionEntry>(partitionKey, QueryOperator.Equal, new string[] { habitId });
            if (habits == null || !habits.Any())
            {
                throw new BadHttpRequestException($"Habit with ID {habitId} was not found in the database.");
            }
            return habits.Single();
        }
    }
}
