using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Diyalo.Api.Data;

namespace Diyalo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "SuperAdmin")]
    public class MigrateController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        public MigrateController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult RunMigration()
        {
            try
            {
                _dbContext.Database.Migrate();
                return Ok(new { success = true, message = "Migration applied successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
