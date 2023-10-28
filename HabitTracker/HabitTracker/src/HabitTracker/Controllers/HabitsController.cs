using HabitTracker.DynamoDb.Models;
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

        private static string GetAuthorizationHeader(HttpRequest request)
        {
            return request.Headers["Authorization"].First();
        }
    }
}
