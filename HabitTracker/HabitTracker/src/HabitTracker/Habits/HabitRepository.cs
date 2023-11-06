﻿using Amazon.DynamoDBv2.DocumentModel;
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

        public async Task<List<HabitDefinition>> GetHabitDefinitions(string authorizationHeader)
        {
            var userId = await _userInfoGetter.GetUserIdAsync(authorizationHeader);
            var partitionKey = new HabitPartitionKey
            {
                UserId = userId,
                ItemType = HabitDefinitionEntry.ItemType
            };
            var habitDefinitionEntries = await _dynamoDbContext.QueryWithEmptyBeginsWithAsync<HabitDefinitionEntry>(partitionKey);
            var habitDefinitions = habitDefinitionEntries.Select(entry => entry.Convert()).ToList();
            return habitDefinitions;
        }

        // TODO: add retry logic to protect agains concurrency.
        public async Task RegisterDoneHabit(string authorizationHeader, DoneHabitRequest request)
        {
            var userId = await _userInfoGetter.GetUserIdAsync(authorizationHeader);
            _ = await GetHabitDefinitionAsync(userId, request.HabitId);
            var date = request.Date;
            var pointer = new HabitMonthRecordPointer(request.HabitId, date.Year, date.Month);
            var habitMonthRecordEntries = await GetHabitMonthRecordEntries(userId, pointer);
            if (habitMonthRecordEntries == null || !habitMonthRecordEntries.Any())
            {
                var newEntry = CreateHabitMonthRecordEntry(userId, request);
                await _dynamoDbContext.SaveAsync(newEntry);
                return;
            }
            var updatedEntry = habitMonthRecordEntries.Single();
            if (updatedEntry.Dates.Contains(date.Day))
                return;
            updatedEntry.Dates.Add(date.Day);
            await _dynamoDbContext.SaveAsync(updatedEntry);
        }

        private HabitMonthRecordEntry CreateHabitMonthRecordEntry(string userId, DoneHabitRequest request)
        {
            return new HabitMonthRecordEntry
            {
                PartitionKey = HabitMonthRecordEntry.CreatePartitionKey(userId),
                Pointer = new HabitMonthRecordPointer(request.HabitId, request.Date.Year, request.Date.Month),
                Dates = new List<int> { request.Date.Day }
            };
        }

        // TODO: add retry logic to protect against concurrency.
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
            var updatedEntry = habitMonthRecordEntries.Single();
            if (updatedEntry.Dates.Remove(date.Day))
            {
                if (updatedEntry.Dates.Any())
                {
                    await _dynamoDbContext.SaveAsync(updatedEntry);
                } else
                {
                    await _dynamoDbContext.DeleteAsync(updatedEntry);
                }                
                return;
            }
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
            var habitDefinitions = await GetHabitDefinitions(authorizationHeader);
            var end = DateTime.Now;
            var start = end.AddDays(-6);
            var pastWeeksDates = GetDatesBetween(start, end);
            var targetYearMonths = pastWeeksDates.DistinctBy(date => date.Month).ToList();

            var habitRecords = new List<HabitRecord>();
            foreach (var habitDefinition in habitDefinitions)
            {
                var habitMonthRecordEntries = await GetAllHabitMonthRecordEntriesAsync(userId, habitDefinition.HabitId);
                var allTimeDoneDatesCount = habitMonthRecordEntries.Select(entry => entry.Dates.Count).Sum();
                var doneDates = new List<Date>();
                targetYearMonths.ForEach(yearMonth =>
                {
                    var habitMonthRecordEntry = habitMonthRecordEntries.Where(entry => entry.Pointer.Year == yearMonth.Year && entry.Pointer.Month == yearMonth.Month);
                    if (habitMonthRecordEntry.Any())
                    {
                        var targetDates = pastWeeksDates
                        .Where(date => date.Year == yearMonth.Year && date.Month == yearMonth.Month)
                        .Select(date => date.Day);
                        var dates = habitMonthRecordEntry.Single().Dates.Where(date => targetDates.Contains(date)).Select(date => new Date(yearMonth.Year, yearMonth.Month, date));
                        doneDates.AddRange(dates);
                    }
                });
                habitRecords.Add(new HabitRecord(habitDefinition, allTimeDoneDatesCount, new Date(start), new Date(end), doneDates));
            }
            return habitRecords;
        }

        private async Task<List<HabitMonthRecordEntry>> GetAllHabitMonthRecordEntriesAsync(string userId, string habitId)
        {
            var partitionKey = HabitMonthRecordEntry.CreatePartitionKey(userId);
            var sortKeyValues = new HabitMonthRecordPointer[] { new HabitMonthRecordPointer(habitId) };
            var habitMonthRecordEntries = await _dynamoDbContext.QueryAsync<HabitMonthRecordEntry>(partitionKey, QueryOperator.BeginsWith, sortKeyValues);
            return habitMonthRecordEntries;
        }

        private List<Date> GetDatesBetween(DateTime start, DateTime end)
        {
            var dates = new List<Date>();
            for (var date = start; date <= end; date = date.AddDays(1))
            {
                dates.Add(new Date(date));
            }
            return dates;
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
