using IMS.Api.Data;
using IMS.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HelperOpportunitiesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HelperOpportunitiesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("providers")]
        public async Task<IActionResult> GetProviders()
        {
            var items = await _context.helper_providers
                .AsNoTracking()
                .Select(x => new HelperProviderDto
                {
                    id = x.id,
                    name = x.name,
                    sector = x.sector,
                    email = x.email,
                    phone = x.phone,
                    city = x.city,
                    website_url = x.website_url,
                    description = x.description
                })
                .OrderBy(x => x.name)
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? q = null)
        {
            var sql = """
                SELECT
                    o.id,
                    o.provider_id,
                    o.title,
                    p.name AS provider_name,
                    o.location,
                    o.work_mode,
                    o.source,
                    o.description,
                    o.deadline_at,
                    o.status::text AS status
                FROM helper_opportunities o
                LEFT JOIN helper_providers p ON p.id = o.provider_id
                WHERE
                    (@q IS NULL OR
                     LOWER(o.title) LIKE LOWER('%' || @q || '%') OR
                     LOWER(COALESCE(p.name, '')) LIKE LOWER('%' || @q || '%') OR
                     LOWER(COALESCE(o.location, '')) LIKE LOWER('%' || @q || '%') OR
                     LOWER(COALESCE(o.source, '')) LIKE LOWER('%' || @q || '%'))
                ORDER BY o.deadline_at DESC NULLS LAST, o.title;
                """;

            var items = await _context.Database.SqlQueryRaw<HelperOpportunityListItemDto>(
                sql,
                new Npgsql.NpgsqlParameter("q", (object?)q ?? DBNull.Value))
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var sql = """
                SELECT
                    o.id,
                    o.provider_id,
                    o.title,
                    p.name AS provider_name,
                    o.location,
                    o.work_mode,
                    o.source,
                    o.description,
                    o.deadline_at,
                    o.status::text AS status
                FROM helper_opportunities o
                LEFT JOIN helper_providers p ON p.id = o.provider_id
                WHERE o.id = @id;
                """;

            var opportunity = await _context.Database.SqlQueryRaw<HelperOpportunityDetailsDto>(
                sql,
                new Npgsql.NpgsqlParameter("id", id))
                .FirstOrDefaultAsync();

            if (opportunity == null)
                return NotFound(new { message = "Opportunity not found." });

            opportunity.requirements = await _context.helper_opportunity_requirements
                .AsNoTracking()
                .Where(x => x.opportunity_id == id)
                .OrderBy(x => x.sort_order)
                .Select(x => x.requirement_text)
                .ToListAsync();

            opportunity.tasks = await _context.helper_opportunity_tasks
                .AsNoTracking()
                .Where(x => x.opportunity_id == id)
                .OrderBy(x => x.sort_order)
                .Select(x => x.task_title)
                .ToListAsync();

            return Ok(opportunity);
        }
    }
}