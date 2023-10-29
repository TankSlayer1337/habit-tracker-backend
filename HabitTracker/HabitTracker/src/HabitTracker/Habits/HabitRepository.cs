using Amazon.DynamoDBv2.DocumentModel;
using HabitTracker.Controllers;
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

        public async Task<List<HabitDefinitionEntry>> GetHabits(string authorizationHeader)
        {
            var userId = await _userInfoGetter.GetUserIdAsync(authorizationHeader);
            var habits = await _dynamoDbContext.QueryWithEmptyBeginsWithAsync<HabitDefinitionEntry>(userId);
            return habits;
        }

        public async Task RegisterDoneHabit(string authorizationHeader, DoneHabitRequest request)
        {
            var userId = await _userInfoGetter.GetUserIdAsync(authorizationHeader);
            var habits = await _dynamoDbContext.QueryAsync<HabitDefinitionEntry>(userId, QueryOperator.Equal, new string[] { request.HabitId });
            if (habits == null || !habits.Any())
            {
                throw new BadHttpRequestException($"Habit with ID {request.HabitId} was not found in the database.");
            }
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

        public async Task<List<DoneHabitEntry>> GetDoneHabitEntries(string authorizationHeader)
        {
            var userId = await _userInfoGetter.GetUserIdAsync(authorizationHeader);
            var doneHabitEntries = await _dynamoDbContext.QueryAsync<DoneHabitEntry>(userId, QueryOperator.BeginsWith, new DoneHabitPointer[] { new DoneHabitPointer() });
            return doneHabitEntries;
        }
    }
}
