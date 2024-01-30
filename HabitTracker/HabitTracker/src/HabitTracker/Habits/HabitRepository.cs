using Amazon.DynamoDBv2.DocumentModel;
using HabitTracker.Controllers.Outputs;
using HabitTracker.Controllers.Requests;
using HabitTracker.DynamoDb;
using HabitTracker.DynamoDb.Models;
using HabitTracker.UserInfo;

namespace HabitTracker.Habits
{
    public class HabitRepository(UserInfoGetter userInfoGetter, DynamoDbContextWrapper dynamoDbContext)
    {
        private readonly UserInfoGetter _userInfoGetter = userInfoGetter;
        private readonly DynamoDbContextWrapper _dynamoDbContext = dynamoDbContext;

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
            if (habitMonthRecordEntries == null || habitMonthRecordEntries.Count == 0)
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
                    if (latestEntries == null || latestEntries.Count == 0)
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
                    if (entry.Dates.Count != 0)
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
                    if (latestEntries == null || latestEntries.Count == 0)
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
                Dates = [request.Date.Day]
            };
        }

        public async Task DeleteDoneHabit(string authorizationHeader, DoneHabitRequest request)
        {
            var userId = await _userInfoGetter.GetUserIdAsync(authorizationHeader);
            var date = request.Date;
            var pointer = HabitMonthRecordPointer.Create(request);
            var habitMonthRecordEntries = await GetHabitMonthRecordEntries(userId, pointer);
            if (habitMonthRecordEntries == null || habitMonthRecordEntries.Count == 0)
            {
                throw new BadHttpRequestException($"Record of done habit with ID {request.HabitId} and Date {date.Year}-{date.Month}-{date.Day} was not found.");
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
                var allTimeDoneDatesCount = habitMonthRecordEntries.Count != 0 ? habitMonthRecordEntries.Select(entry => entry.Dates.Count).Sum() : 0;
                var doneDates = DoneDateExtractor.GetDoneDatesInRange(habitMonthRecordEntries, start, end);
                var chartData = ChartDataCreator.Create(habitMonthRecordEntries);
                habitRecords.Add(new HabitRecord(definition, allTimeDoneDatesCount, new Date(start), new Date(end), doneDates, chartData));
            }
            return habitRecords;
        }

        private async Task<List<HabitMonthRecordEntry>> GetAllHabitMonthRecordEntriesAsync(string userId, string habitId)
        {
            var partitionKey = HabitMonthRecordEntry.CreatePartitionKey(userId);
            var sortKeyValues = new HabitMonthRecordPointer[] { new(habitId) };
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
            if (habits == null || habits.Count == 0)
            {
                throw new BadHttpRequestException($"Habit with ID {habitId} was not found.");
            }
            return habits.Single();
        }
    }
}
