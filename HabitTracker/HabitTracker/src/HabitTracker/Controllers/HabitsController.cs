﻿using HabitTracker.DynamoDb.Models;
using HabitTracker.Habits;
using Microsoft.AspNetCore.Mvc;

namespace HabitTracker.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HabitsController : ControllerBase
    {
        private readonly HabitRepository _habitRepository;

        public HabitsController(HabitRepository habitRepository)
        {
            _habitRepository = habitRepository;
        }

        [HttpPost]
        public async Task CreateHabit([FromBody] CreateHabitRequest request)
        {
            var authorizationHeader = GetAuthorizationHeader(Request);
            await _habitRepository.CreateHabit(authorizationHeader, request.Name);
        }

        [HttpGet]
        public async Task<List<HabitDefinitionEntry>> GetHabits()
        {
            var authorizationHeader = GetAuthorizationHeader(Request);
            return await _habitRepository.GetHabits(authorizationHeader);
        }

        [HttpPost("done")]
        public async Task RegisterDoneHabit([FromBody] DoneHabitRequest request)
        {
            var authorizationHeader = GetAuthorizationHeader(Request);
            await _habitRepository.RegisterDoneHabit(authorizationHeader, request);
        }

        [HttpDelete("done")]
        public async Task DeleteDoneHabit([FromBody] DoneHabitRequest request)
        {
            var authorizationHeader = GetAuthorizationHeader(Request);
            await _habitRepository.DeleteDoneHabit(authorizationHeader, request);
        }

        [HttpGet("done")]
        public async Task<List<DoneHabitEntry>> GetDoneHabits()
        {
            var authorizationHeader = GetAuthorizationHeader(Request);
            return await _habitRepository.GetDoneHabitEntries(authorizationHeader);
        }

        private static string GetAuthorizationHeader(HttpRequest request)
        {
            return request.Headers["Authorization"].First();
        }
    }
}
