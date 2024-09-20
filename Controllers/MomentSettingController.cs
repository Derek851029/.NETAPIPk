using Dapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PKApp.ConfigOptions;
using PKApp.DIObject;

namespace PKApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MomentSettingController : ControllerBase
    {
        private readonly DapperContext _context;
        private readonly ILogger<MomentSettingController> _logger;

        public MomentSettingController(DapperContext context, ILogger<MomentSettingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetMomentSetting(int id)
        {
            try
            {
                string sql = "";
                IEnumerable<dynamic> returnData;

                using (var conn = _context.CreateConnection())
                {
                    if (id == 0)
                    {
                        sql = @"select *,GROUP_CONCAT(week) as group_week,GROUP_CONCAT(time) as group_time from app_moment a
                            left join app_moment_week b on a.app_moment_id = b.app_moment_id
                            left join app_moment_time c on a.app_moment_id = c.app_moment_id
                            where deleted = 0
                            group by a.app_moment_id";
                        returnData = await conn.QueryAsync(sql);
                    }
                    else
                    {
                        sql = @"select *,GROUP_CONCAT(week) as group_week,GROUP_CONCAT(time) as group_time from app_moment a
                            left join app_moment_week b on a.app_moment_id = b.app_moment_id
                            left join app_moment_time c on a.app_moment_id = c.app_moment_id
                            where a.app_moment_id = @id and deleted = 0";
                        returnData = await conn.QueryAsync(sql, new { id = id });
                    }

                }
                return Ok(returnData.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> NewData(FrontEndData json)
        {
            try
            {
                string sql = "";
                var data = JsonConvert.DeserializeObject<CurrentMomentData>(json.DataInfo);
                dynamic returnData = "";

                using (var conn = _context.CreateConnection())
                {
                    switch (json.Type)
                    {
                        case "new":
                            sql = @"insert into app_moment(name,created,created_aid, changed, changed_aid, deleted) 
                                values(@name, @created, @created_aid, @changed, @changed_aid, @deleted);
                                SELECT CAST(LAST_INSERT_ID() AS UNSIGNED INTEGER)";
                            var app_moment_id = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                            {
                                name = data.Name,
                                created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                created_aid = HttpContext.Request.Cookies["AccountAid"],
                                changed = 0,
                                changed_aid = 0,
                                deleted = 0
                            });

                            foreach (var week in data.Week)
                            {
                                sql = @"insert into app_moment_week(app_moment_id, week)values(@app_moment_id,@week)";
                                await conn.QueryAsync(sql, new { app_moment_id = app_moment_id, week = Convert.ToInt32(week) });
                            }

                            foreach (var time in data.Time)
                            {
                                sql = @"insert into app_moment_time(app_moment_id, time)values(@app_moment_id,@time)";
                                await conn.QueryAsync(sql, new { app_moment_id = app_moment_id, time = Convert.ToInt32(time) });
                            }
                            break;

                        case "edit":
                            sql = @"update app_moment set name = @name, changed = @changed, changed_aid = @changed_aid 
                                    where app_moment_id = @app_moment_id";
                            await conn.QueryAsync(sql, new
                            {
                                name = data.Name,
                                changed = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                changed_aid = HttpContext.Request.Cookies["AccountAid"],
                                app_moment_id = json.App_moment_id
                            });

                            sql = @"delete from app_moment_week where app_moment_id = @app_moment_id";
                            await conn.QueryAsync(sql, new { app_moment_id = json.App_moment_id });

                            sql = @"delete from app_moment_time where app_moment_id = @app_moment_id";
                            await conn.QueryAsync(sql, new { app_moment_id = json.App_moment_id });

                            foreach (var week in data.Week)
                            {
                                sql = @"insert into app_moment_week(app_moment_id, week)values(@app_moment_id,@week)";
                                await conn.QueryAsync(sql, new { app_moment_id = json.App_moment_id, week = Convert.ToInt32(week) });
                            }

                            foreach (var time in data.Time)
                            {
                                sql = @"insert into app_moment_time(app_moment_id, time)values(@app_moment_id,@time)";
                                await conn.QueryAsync(sql, new { app_moment_id = json.App_moment_id, time = Convert.ToInt32(time) });
                            }
                            break;

                        case "delete":
                            sql = @"update app_moment set deleted = 1 where app_moment_id = @app_moment_id";
                            await conn.QueryAsync(sql, new { app_moment_id = json.App_moment_id });
                            break;
                    }

                }
                return Ok(returnData);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }
    }
}

public class CurrentMomentData
{
    public string? Name { get; set; }
    public string[]? Week { get; set; }
    public string[]? Time { get; set; }

    public string? CreateDate { get; set; }
}