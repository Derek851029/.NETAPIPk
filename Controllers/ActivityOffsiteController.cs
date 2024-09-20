using Dapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PKApp.ConfigOptions;
using PKApp.DIObject;
using PKApp.Services;

namespace PKApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActivityOffsiteController : ControllerBase
    {
        private readonly ILogger<ActivityOffsiteController> _logger;
        private readonly DapperContext _context;
        private readonly IFilesService _filesService;

        public ActivityOffsiteController(ILogger<ActivityOffsiteController> logger, DapperContext context, IFilesService filesService)
        {
            _logger = logger;
            _context = context;
            _filesService = filesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetActivity(string type, int id)
        {
            try
            {
                string sql = "";
                IEnumerable<dynamic> returnData = null;

                using (var conn = _context.CreateConnection2())
                {

                    switch (type)
                    {
                        case "list":
                            sql = @"SELECT * FROM activity WHERE deleted = 0 order by created desc";

                            returnData = await conn.QueryAsync(sql);
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
        public async Task<IActionResult> NewMessage(FrontEndData json)
        {
            try
            {
                string sql = "";
                string type = json.Type;
                var dataInfo = JsonConvert.DeserializeObject<ActivityOffsiteData>(json.DataInfo);
                dynamic returnData = "Success";
                int image_fid = 0;
                int icon_fid = 0;
                int cover_fid = 0;
                using (var conn = _context.CreateConnection2())
                {
                    switch (type)
                    {
                        case "new":
                            sql = @"INSERT INTO activity(activity_type,title,start_time,end_time,chance,play_interval,play_target,is_free,
                                    icon_fid,push_icon_fid,push_title,status,deleted,created,modify)
                                    VALUES(@activity_type,@title,@start_time,@end_time,@chance,@play_interval,@play_target,@is_free,
                                    @icon_fid,@push_icon_fid,@push_title,@status,@deleted,@created,@modify);";

                            await conn.QueryAsync(sql, new
                            {
                                activity_type = dataInfo.Activity_type,
                            });
                            break;

                        case "status":
                            sql = @"UPDATE app_activity set status = @status where activity_id = @activity_id";
                            returnData = await conn.QueryAsync<int>(sql, new
                            {
                                status = dataInfo.Status,
                                activity_id = json.ActivityOffsiteId,
                            });
                            break;

                        case "delete":
                            sql = @"UPDATE app_activity SET deleted = @status where activity_id = @activity_id";
                            returnData = await conn.QueryAsync<int>(sql, new
                            {
                                status = dataInfo.Status,
                                activityID = json.ActivityOffsiteId,
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
    }
}

public class ActivityOffsiteData
{
    public string? Activity_type { get; set; }
    public string? Title { get; set; }
    public string[]? Time { get; set; }
    public int? Chance { get; set; }
    public int? Play_interval { get; set; }
    public int? Play_target { get; set; }
    public int? is_free { get; set; }
    public string? Activity_image { get; set; }
    public string? App_image { get; set; }
    public string? Push_title { get; set; }
    public int? Status { get; set; }
}
