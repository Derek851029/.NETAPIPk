using Dapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PKApp.ConfigOptions;
using PKApp.DIObject;

namespace PKApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly ILogger<RolesController> _logger;
        private readonly DapperContext _context;

        public RolesController(ILogger<RolesController> logger, DapperContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles(int id)
        {
            try
            {
                string sql = "";
                using (var conn = _context.CreateConnection())
                {
                    if (id == 0)
                    {
                        sql = @"select * from administrators_roles_manage where rid > 1 order by role_created asc";

                        var rolesList = await conn.QueryAsync(sql);
                        return Ok(rolesList.ToList());
                    }
                    else
                    {
                        sql = @"select *,GROUP_CONCAT(b.pid) as group_pid from administrators_roles_manage a
                                left join administrators_roles_permissions b on a.rid = b.rid
                                where a.rid = @id
                                group by a.rid";

                        var rolesData = await conn.QueryAsync(sql, new { id = id });
                        return Ok(rolesData.ToList());
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> NewRoles(FrontEndData json)
        {
            try
            {
                string sql = "";
                string type = json.Type;
                var dataInfo = JsonConvert.DeserializeObject<RoleData>(json.DataInfo);
                dynamic returnData = "Success";
                using (var conn = _context.CreateConnection())
                {
                    switch (type)
                    {
                        case "new":
                            sql = @"insert into administrators_roles_manage(role_created, role_created_aid, role_modify, role_modify_aid,
                                    role_title) values(@role_created, @role_created_aid, @role_modify, @role_modify_aid, @role_title);
                                    SELECT CAST(LAST_INSERT_ID() AS UNSIGNED INTEGER)";

                            var rid = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                            {
                                role_created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                role_created_aid = HttpContext.Request.Cookies["AccountAid"],
                                role_modify = 0,
                                role_modify_aid = 0,
                                role_title = dataInfo.Role_title,
                            });

                            sql = @"insert into administrators_roles_permissions(rid, pid) values(@rid, @pid)";

                            foreach (var pid in dataInfo.Permission)
                            {
                                await conn.QueryAsync(sql, new { rid = rid, pid = pid });
                            }
                            break;

                        case "edit":
                            sql = @"update administrators_roles_manage set role_modify = @role_modify, role_modify_aid = @role_modify_aid, role_title = @role_title
                                    where rid = @rid";
                            await conn.QueryAsync(sql, new
                            {
                                role_modify = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                role_modify_aid = HttpContext.Request.Cookies["AccountAid"],
                                role_title = dataInfo.Role_title,
                                rid = json.Rid
                            });

                            sql = @"delete from administrators_roles_permissions where rid = @rid";
                            await conn.QueryAsync(sql, new { rid = json.Rid });

                            sql = @"insert into administrators_roles_permissions(rid, pid) values(@rid, @pid)";

                            foreach (var pid in dataInfo.Permission)
                            {
                                await conn.QueryAsync(sql, new { rid = json.Rid, pid = pid });
                            }
                            break;
                    }

                    return Ok(returnData);
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

public class RoleData
{
    public string? Role_title { get; set; }
    public string[]? Permission { get; set; }
    public string? CreateDate { get; set; }
}
