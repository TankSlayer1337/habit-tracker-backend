using Amazon.DynamoDBv2.DocumentModel;
using HabitTracker.Controllers.Outputs;
using HabitTracker.Controllers.Requests;
using HabitTracker.DynamoDb;
using HabitTracker.DynamoDb.Models;
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
            var updatedEntry = habitDefinitionEntry.CopyWithNewValues(request);
            await _dynamoDbContext.SaveAsync(updatedEntry);
        }

        public async Task DeleteHabit(string authorizationHeader, string habitId)
        {
            var userId = await _userInfoGetter.GetUserIdAsync(authorizationHeader);
            var habitDefinitionEntry = await GetHabitDefinitionAsync(userId, habitId);
            var doneHabitEntries = await GetDoneHabitEntriesAsync(userId);
            var affectedDoneHabitEntries = doneHabitEntries.Where(entry => entry.DoneHabitPointer.HabitId == habitId);
            foreach (var entry in affectedDoneHabitEntries)
            {
                await _dynamoDbContext.DeleteAsync(entry);
            }
            await _dynamoDbContext.DeleteAsync(habitDefinitionEntry);
        }

        public async Task<List<HabitDefinition>> GetHabitDefinitions(string authorizationHeader)
        {
            var userId = await _userInfoGetter.GetUserIdAsync(authorizationHeader);
            var habitDefinitionEntries = await _dynamoDbContext.QueryWithEmptyBeginsWithAsync<HabitDefinitionEntry>(userId);
            var habitDefinitions = habitDefinitionEntries.Select(entry => entry.Convert()).ToList();
            return habitDefinitions;
        }

        public async Task RegisterDoneHabit(string authorizationHeader, DoneHabitRequest request)
        {
            var userId = await _userInfoGetter.GetUserIdAsync(authorizationHeader);
            _ = await GetHabitDefinitionAsync(userId, request.HabitId);
            var doneHabitEntry = DoneHabitEntry.Create(userId, request);
            await _dynamoDbContext.SaveAsync(doneHabitEntry);
        }

        public async Task DeleteDoneHabit(string authorizationHeader, DoneHabitRequest request)
        {
            var userId = await _userInfoGetter.GetUserIdAsync(authorizationHeader);
            var pointer = new DoneHabitPointer { Date = request.Date, HabitId = request.HabitId };
            var doneHabitEntries = await _dynamoDbContext.QueryAsync<DoneHabitEntry>(userId, QueryOperator.Equal, new DoneHabitPointer[] { pointer });
            if (doneHabitEntries == null || !doneHabitEntries.Any())
            {
                throw new BadHttpRequestException($"{nameof(DoneHabitEntry)} with ID {request.HabitId} and Date {request.Date} was not found in the database.");
            }
            var doneHabitEntry = doneHabitEntries.Single();
            await _dynamoDbContext.DeleteAsync(doneHabitEntry);
        }

        public async Task<List<DoneHabitPointer>> GetDoneHabits(string authorizationHeader)
        {
            var userId = await _userInfoGetter.GetUserIdAsync(authorizationHeader);
            var doneHabitEntries = await GetDoneHabitEntriesAsync(userId);
            var pointers = doneHabitEntries.Select(entry => entry.DoneHabitPointer).ToList();
            return pointers;
        }

        public async Task<List<HabitRecord>> GetHabitRecords(string authorizationHeader)
        {
            var userId = await _userInfoGetter.GetUserIdAsync(authorizationHeader);
            var habitDefinitionEntries = await _dynamoDbContext.QueryWithEmptyBeginsWithAsync<HabitDefinitionEntry>(userId);
            var doneHabitEntries = await GetDoneHabitEntriesAsync(userId);
            var habitRecords = habitDefinitionEntries.Select(habitDefinitionEntry =>
            new HabitRecord
            {
                HabitId = habitDefinitionEntry.HabitId,
                Name = habitDefinitionEntry.Name,
                Dates = doneHabitEntries
                    .Where(doneHabitEntry => doneHabitEntry.DoneHabitPointer.HabitId == habitDefinitionEntry.HabitId)
                    .Select(doneHabitEntry => doneHabitEntry.DoneHabitPointer.Date).ToList()
            }).ToList();
            return habitRecords;
        }

        private async Task<List<DoneHabitEntry>> GetDoneHabitEntriesAsync(string userId)
        {
            return await _dynamoDbContext.QueryAsync<DoneHabitEntry>(userId, QueryOperator.BeginsWith, new DoneHabitPointer[] { new DoneHabitPointer() });
        }

        private async Task<HabitDefinitionEntry> GetHabitDefinitionAsync(string userId, string habitId)
        {
            var habits = await _dynamoDbContext.QueryAsync<HabitDefinitionEntry>(userId, QueryOperator.Equal, new string[] { habitId });
            if (habits == null || !habits.Any())
            {
                throw new BadHttpRequestException($"Habit with ID {habitId} was not found in the database.");
            }
            return habits.Single();
        }
    }
}
