using HabitTracker.Controllers.Outputs;
using HabitTracker.Controllers.Requests;
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

        [HttpGet("chart/{habitId}")]
        public async Task<ChartData> GetChartData(string habitId)
        {
            var authorizationHeader = GetAuthorizationHeader(Request);
            return await _habitRepository.GetChartData(authorizationHeader, habitId);
        }

        [HttpGet("records")]
        public async Task<List<HabitRecord>> GetHabitRecords()
        {
            var authorizationHeader = GetAuthorizationHeader(Request);
            return await _habitRepository.GetHabitRecordsForPastWeek(authorizationHeader);
        }

        [HttpPost]
        public async Task CreateHabit([FromBody] CreateHabitRequest request)
        {
            var authorizationHeader = GetAuthorizationHeader(Request);
            await _habitRepository.CreateHabit(authorizationHeader, request.Name);
        }

        [HttpPost("update")]
        public async Task UpdateHabit([FromBody] UpdateHabitRequest request)
        {
            var authorizationHeader = GetAuthorizationHeader(Request);
            await _habitRepository.UpdateHabit(authorizationHeader, request);
        }

        [HttpDelete("{habitId}")]
        public async Task DeleteHabit(string habitId)
        {
            var authorizationHeader = GetAuthorizationHeader(Request);
            await _habitRepository.DeleteHabit(authorizationHeader, habitId);
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

        private static string GetAuthorizationHeader(HttpRequest request)
        {
            return request.Headers["Authorization"].First();
        }
    }
}