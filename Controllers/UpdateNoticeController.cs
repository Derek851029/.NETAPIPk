using Dapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PKApp.ConfigOptions;
using PKApp.DIObject;

namespace PKApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UpdateNoticeController : ControllerBase
    {
        private readonly ILogger<UpdateNoticeController> _logger;
        private readonly DapperContext _context;

        public UpdateNoticeController(ILogger<UpdateNoticeController> logger, DapperContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetActivity(string type, int version, int id)
        {
            try
            {
                string sql = "";
                IEnumerable<dynamic> returnData = null;

                using (var conn = _context.CreateConnection())
                {
                    switch (type)
                    {
                        case "list":
                            sql = @"SELECT * FROM app_update_notice WHERE deleted = 0";

                            returnData = await conn.QueryAsync(sql);
                            break;

                        case "edit":
                            sql = @"SELECT * FROM app_update_notice WHERE app_update_notice_id = @app_update_notice_id";

                            returnData = await conn.QueryAsync(sql, new { app_update_notice_id = id });
                            break;

                        case "editVersion":
                            sql = @"SELECT * FROM app_update_notice_version WHERE app_update_notice_id = @app_update_notice_id AND version = @version";

                            returnData = await conn.QueryAsync(sql, new { version = version, app_update_notice_id = id });
                            break;

                        case "version":
                            sql = @"SELECT * FROM app_update_notice_version WHERE app_update_notice_id = @app_update_notice_id order by ver_created desc";

                            returnData = await conn.QueryAsync(sql, new { app_update_notice_id = id });
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

        [HttpPost]
        public async Task<IActionResult> Post(FrontEndData json)
        {
            try
            {
                string sql = "";
                string type = json.Type;
                var dataInfo = JsonConvert.DeserializeObject<UpdateNoticeData>(json.DataInfo);
                dynamic returnData = "Success";

                long todayUnix = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                var userAid = HttpContext.Request.Cookies["AccountAid"];

                using (var conn = _context.CreateConnection())
                {
                    switch (type)
                    {
                        case "edit":
                            int version = await InsertVersion((int)json.UpdateNoticeID);

                            sql = @"UPDATE app_update_notice SET version = @version, app_version = @app_version,tips = @tips,
                                    is_force = @is_force, changed = @changed, changed_aid = @changed_aid 
                                    WHERE app_update_notice_id = @app_update_notice_id;";
                            await conn.QueryAsync(sql, new
                            {
                                version = version + 1,
                                app_version = dataInfo.App_version,
                                tips = dataInfo.Tips,
                                is_force = dataInfo.Is_force,
                                start_push_time = todayUnix + 3600,
                                changed = todayUnix,
                                changed_aid = userAid,
                                app_update_notice_id = json.UpdateNoticeID,
                            });
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

        public async Task<int> InsertVersion(int id)
        {
            try
            {
                string sql = "";
                List<string> jsonStr = new List<string>();

                using (var conn = _context.CreateConnection())
                {

                    sql = @"INSERT INTO app_update_notice_version(app_update_notice_id,version,device,app_version,tips,is_notice,is_force,
                            ver_created,ver_created_aid)
                            SELECT app_update_notice_id AS app_update_notice_id, version AS version, device AS device, app_version AS app_version, tips AS tips, @is_notice AS is_notice, 
                            is_force AS is_force, @ver_created AS ver_created, @ver_created_aid AS ver_created_aid FROM app_update_notice WHERE app_update_notice_id = @app_update_notice_id";

                    await conn.QueryAsync(sql, new
                    {
                        app_update_notice_id = id,
                        is_notice = 0,
                        ver_created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                        ver_created_aid = HttpContext.Request.Cookies["AccountAid"],
                    });

                    sql = @"SELECT version FROM app_update_notice WHERE app_update_notice_id = @app_update_notice_id";

                    var getVersion = await conn.QuerySingleOrDefaultAsync(sql, new { app_update_notice_id = id });

                    return (int)getVersion.version;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return 0;
            }

        }
    }
}

public class UpdateNoticeData
{
    public string? App_version { get; set; }
    public string? Tips { get; set; }
    public int? Is_force { get; set; }
}
