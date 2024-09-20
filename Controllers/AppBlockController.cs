using Dapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PKApp.ConfigOptions;
using PKApp.DIObject;

namespace PKApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppBlockController : ControllerBase
    {
        private readonly ILogger<AppBlockController> _logger;
        private readonly DapperContext _context;

        public AppBlockController(ILogger<AppBlockController> logger, DapperContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                int todayUnix = (int)DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();

                using (var conn = _context.CreateConnection())
                {
                    string sql = @"SELECT * FROM app_block ORDER BY sort ASC";
                    var data = await conn.QueryAsync(sql);
                    List<dynamic> newList = data.Select(item =>
                    {
                        if (item.end_time < todayUnix)
                        {
                            DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(todayUnix).LocalDateTime;

                            DateTime newStarTime = dateTime.AddMinutes(5);
                            DateTime newEndTime = dateTime.AddHours(72);

                            item.title = item.next_title;
                            item.sub_title = item.next_sub_title;
                            item.start_time = item.next_start_time;
                            item.end_time = item.next_end_time;
                            item.next_title = "";
                            item.next_sub_title = "";
                            item.next_start_time = (object)new DateTimeOffset(newStarTime, TimeSpan.FromHours(8)).ToUnixTimeSeconds();
                            item.next_end_time = (object)new DateTimeOffset(newEndTime, TimeSpan.FromHours(8)).ToUnixTimeSeconds(); ;
                        }
                        return item;
                    }).ToList();

                    sql = @"UPDATE app_block SET title = @title, sub_title = @sub_title, start_time = @start_time, end_time = @end_time,
                            next_title = @next_title, next_sub_title = @next_sub_title, next_start_time = @next_start_time, next_end_time = @next_end_time
                            WHERE block_name = @block_name";
                    await conn.ExecuteAsync(sql, newList);

                    return Ok(data);
                }
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
                var type = json.Type;
                var dataInfo = JsonConvert.DeserializeObject<AppBlockData>(json.DataInfo);
                dynamic returnData = "Success";
                int todayUnix = (int)DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                string aid = HttpContext.Request.Cookies["AccountAid"];

                using (var conn = _context.CreateConnection())
                {
                    switch (type)
                    {
                        case "now":
                            sql = @"UPDATE app_block SET title = @title, sub_title = @sub_title, start_time = @start_time, end_time = @end_time,
                            changed = @changed, changed_aid = @changed_aid WHERE block_name = @block_name";

                            await conn.QueryAsync(sql, new
                            {
                                title = dataInfo.Title,
                                sub_title = dataInfo.Sub_title,
                                start_time = DateTimeOffset.Parse(dataInfo.Time[0]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                end_time = DateTimeOffset.Parse(dataInfo.Time[1]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                changed = todayUnix,
                                changed_aid = aid,
                                block_name = dataInfo.Block_name,
                            });
                            break;

                        case "next":
                            sql = @"UPDATE app_block SET next_title = @next_title, next_sub_title = @next_sub_title, next_start_time = @next_start_time, next_end_time = @next_end_time,
                            changed = @changed, changed_aid = @changed_aid WHERE block_name = @block_name";

                            await conn.QueryAsync(sql, new
                            {
                                next_title = dataInfo.Next_title,
                                next_sub_title = dataInfo.Next_sub_title,
                                next_start_time = DateTimeOffset.Parse(dataInfo.Next_time[0]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                next_end_time = DateTimeOffset.Parse(dataInfo.Next_time[1]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                changed = todayUnix,
                                changed_aid = aid,
                                block_name = dataInfo.Block_name,
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

public class AppBlockData
{
    public string? Block_name { get; set; }
    public string? Title { get; set; }
    public string? Sub_title { get; set; }
    public string[]? Time { get; set; }
    public string? Next_title { get; set; }
    public string? Next_sub_title { get; set; }
    public string[]? Next_time { get; set; }
}
