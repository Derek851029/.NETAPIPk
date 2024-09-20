using Dapper;
using Microsoft.AspNetCore.Mvc;
using PKApp.DIObject;
using System.Data;

namespace PKApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuController : ControllerBase
    {
        private readonly DapperContext _context;

        public MenuController(DapperContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMenus()
        {
            try
            {
                using (var conn = _context.CreateConnection())
                {
                    var Agent_ID = HttpContext.Request.Cookies["AgentID"];
                    string sql = @"select c.* 
                                        from staffList a
                                        inner join ROLEPROG b
                                        on a.Agent_ID = @Agent_ID and a.Role_id = b.Role_ID
                                        inner join PROGLIST c
                                        on b.TREE_ID = c.TREE_ID
                                        where c.IS_OPEN = 'Y' and a.Agent_Status !='離職'
                                        union all
                                        select * from PROGLIST
                                        where tree_ID in (
                                        Select  c.PARENT_ID
                                        from staffList a
                                        inner join ROLEPROG b
                                        on a.Agent_ID = @Agent_ID and a.Role_id = b.Role_ID
                                        inner join PROGLIST c
                                        on b.TREE_ID = c.TREE_ID
                                        where c.IS_OPEN = 'Y' and a.Agent_Status !='離職' )
                                        Order by c.TREE_ID, LEVEL_ID, SORT_ID";
                    var menus = await conn.QueryAsync(sql, new { Agent_ID = Agent_ID });
                    return Ok(menus.ToList());
                }
            }
            catch (Exception ex)
            {
                using (var conn = _context.CreateConnection())
                {
                    string sql = @"Insert Into SystemLog(PageName, PageFunc, PageLog, EX) 
                                        Values(@PageName, @PageFunc, @PageLog, @EX)";

                    var parameters = new DynamicParameters();
                    parameters.Add("PageName", "MenuController", DbType.String);
                    parameters.Add("PageFunc", "GetMenus", DbType.String);
                    parameters.Add("PageLog", "Get menu list", DbType.String);
                    parameters.Add("Ex", ex.Message, DbType.String);

                    var id = await conn.QuerySingleAsync<int>(sql, parameters);

                    return StatusCode(500, ex.Message);
                }

            }
        }
    }
}
