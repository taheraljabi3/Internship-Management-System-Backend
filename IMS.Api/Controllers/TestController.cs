using IMS.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TestController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("db-check")]
        public async Task<IActionResult> Check()
        {
            var canConnect = await _context.Database.CanConnectAsync();

            return Ok(new
            {
                connected = canConnect,
                database = _context.Database.GetDbConnection().Database
            });
        }
    }
}