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
    public class GreetingController : ControllerBase
    {
        private readonly ILogger<GreetingController> _logger;
        private readonly DapperContext _context;
        private readonly IFilesService _filesService;

        public GreetingController(ILogger<GreetingController> logger, DapperContext context, IFilesService filesService)
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

                using (var conn = _context.CreateConnection())
                {
                    switch (type)
                    {
                        case "list":
                            sql = @"SELECT * FROM app_new_greeting";

                            returnData = await conn.QueryAsync(sql);
                            break;

                        case "edit":
                            sql = @"SELECT * FROM app_new_greeting WHERE app_greeting_id = @app_greeting_id";

                            returnData = await conn.QueryAsync(sql, new { app_greeting_id = id });
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
                var dataInfo = JsonConvert.DeserializeObject<GreetingData>(json.DataInfo);
                dynamic returnData = "Success";

                long todayUnix = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                var userAid = HttpContext.Request.Cookies["AccountAid"];

                using (var conn = _context.CreateConnection())
                {
                    switch (type)
                    {
                        case "new":
                            sql = @"SELECT * FROM app_new_greeting";
                            var timeData = await conn.QueryAsync(sql);
                            var check = CheckTime(timeData.ToList(), dataInfo.Time);
                            if (check.isOverlap)
                            {
                                return Ok(check.app_greeting_id);
                            }

                            sql = @"INSERT INTO app_new_greeting (greeting_name,start_time, end_time, created, created_aid ) 
                                    VALUES(@greeting_name,@start_time, @end_time, @created, @created_aid)";

                            await conn.QueryAsync(sql, new
                            {
                                greeting_name = dataInfo.Greeting_name,
                                start_time = DateTimeOffset.Parse(dataInfo.Time[0]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                end_time = DateTimeOffset.Parse(dataInfo.Time[1]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                created_aid = HttpContext.Request.Cookies["AccountAid"],
                            });
                            break;
                        case "edit":

                            sql = @"UPDATE app_new_greeting SET greeting_name = @greeting_name,start_time = @start_time,end_time = @end_time,
                                    created = @created, created_aid = @created_aid
                                    WHERE app_greeting_id = @app_greeting_id";
                            await conn.QueryAsync(sql, new
                            {
                                app_greeting_id = json.GreetingID,
                                greeting_name = dataInfo.Greeting_name,
                                start_time = DateTimeOffset.Parse(dataInfo.Time[0]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                end_time = DateTimeOffset.Parse(dataInfo.Time[1]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                created_aid = HttpContext.Request.Cookies["AccountAid"],
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

        private dynamic CheckTime(List<dynamic> timeData, string[] time)
        {
            int f_start_hour = DateTime.Parse(time[0]).Hour;
            int f_start_min = DateTime.Parse(time[0]).Minute;
            int f_end_hour = DateTime.Parse(time[1]).Hour;
            int f_end_min = DateTime.Parse(time[1]).Minute;

            bool isOverlap = true;
            long app_greeting_id = 0;
            for (int i = 0; i < timeData.Count; i++)
            {
                var item = timeData[i];
                int db_start_hour = TimeZoneInfo.ConvertTimeFromUtc(DateTimeOffset.FromUnixTimeSeconds(item.start_time).UtcDateTime, TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time")).Hour;
                int db_start_min = TimeZoneInfo.ConvertTimeFromUtc(DateTimeOffset.FromUnixTimeSeconds(item.start_time).UtcDateTime, TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time")).Minute;
                int db_end_hour = TimeZoneInfo.ConvertTimeFromUtc(DateTimeOffset.FromUnixTimeSeconds(item.end_time).UtcDateTime, TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time")).Hour;
                int db_end_min = TimeZoneInfo.ConvertTimeFromUtc(DateTimeOffset.FromUnixTimeSeconds(item.end_time).UtcDateTime, TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time")).Minute;


                TimeSpan frontendStartTime = new TimeSpan(f_start_hour, f_start_min, 0);
                TimeSpan frontendEndTime = new TimeSpan(f_end_hour, f_end_min, 0);
                TimeSpan dbStartTime = new TimeSpan(db_start_hour, db_start_min, 0);
                TimeSpan dbEndTime = new TimeSpan(db_end_hour, db_end_min, 0);

                isOverlap = !(frontendEndTime < dbStartTime || frontendStartTime > dbEndTime);
                if (isOverlap)
                {
                    app_greeting_id = item.app_greeting_id;
                    break;
                }
            }
            return new
            {
                isOverlap = isOverlap,
                app_greeting_id = app_greeting_id
            };
        }
    }
}

public class GreetingData
{
    public string? Greeting_name { get; set; }
    public string[]? Time { get; set; }
}
