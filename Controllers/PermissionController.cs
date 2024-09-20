using Dapper;
using Microsoft.AspNetCore.Mvc;
using PKApp.DIObject;

namespace PKApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PermissionController : ControllerBase
    {
        private readonly ILogger<PermissionController> _logger;
        private readonly DapperContext _context;

        public PermissionController(ILogger<PermissionController> logger, DapperContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetPermission()
        {
            try
            {
                string sql = @"select * from administrators_roles_permissions_manage";
                using (var conn = _context.CreateConnection())
                {
                    var permissionList = await conn.QueryAsync(sql);
                    return Ok(permissionList.ToList());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }
    }
}
