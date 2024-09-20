using Dapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PKApp.ConfigOptions;
using PKApp.DIObject;

namespace PKApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeliveryPushController : ControllerBase
    {
        private readonly DapperContext _context;
        private readonly ILogger _logger;

        public DeliveryPushController(DapperContext context, ILogger<DeliveryPushController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetData(string type, int id)
        {
            try
            {
                string sql = "";
                IEnumerable<dynamic> returnData = null;

                using (var conn = _context.CreateConnection())
                {
                    sql = @"SELECT * FROM waiting_delivery_push where type = @type and primary_id = @primary_id";
                    returnData = await conn.QueryAsync(sql, new { type = type, primary_id = id });

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
        public async Task<IActionResult> NewDeliveryPush(FrontEndData json)
        {
            try
            {
                string sql = "";
                var dataInfo = JsonConvert.DeserializeObject<MessageData>(json.DataInfo);
                dynamic returnData = "Success";

                using (var conn = _context.CreateConnection())
                {
                    sql = @"INSERT INTO waiting_delivery_push(delivery_status,type,primary_id,start_push_time,member_json,
                            tag_json,delivery_count,start_schedule_time,start_delivery_time,complete_time,created,created_aid)
                            VALUES(@delivery_status,@type,@primary_id,@start_push_time,@member_json,@tag_json,@delivery_count,
                            @start_schedule_time,@start_delivery_time,@complete_time,@created,@created_aid)";
                    await conn.QueryAsync(sql, new
                    {
                        delivery_status = 1,
                        type = json.Type,
                        primary_id = json.ID,
                        start_push_time = dataInfo.PushTime.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                        member_json = dataInfo.PushData != null ? "[" + dataInfo.PushData + "]" : "",
                        tag_json = dataInfo.PushData != null ? "[" + dataInfo.PushData + "]" : "",
                        delivery_count = 0,
                        start_schedule_time = dataInfo.PushTime.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                        start_delivery_time = 0,
                        complete_time = 0,
                        created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                        created_aid = HttpContext.Request.Cookies["AccountAid"],
                    });
                };
                return Ok(returnData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteDeliveryPush(FrontEndData json)
        {
            try
            {
                dynamic returnData = "Success";
                string sql = "";

                using (var conn = _context.CreateConnection())
                {
                    sql = @"select * from waiting_delivery_push where delivery_status = 2 and delivery_id = @delivery_id";
                    var check = await conn.QueryAsync(sql, new { delivery_id = json.DeliveryId });

                    if (check.Any())
                    {
                        returnData = "Error";
                    }
                    else
                    {
                        sql = @"delete from waiting_delivery_push where type = @type and delivery_id = @delivery_id";
                        await conn.QueryAsync(sql, new { type = json.Type, delivery_id = json.DeliveryId });
                    }
                };
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

public class DeliveryPushData
{
    public DateTimeOffset PushTime { get; set; }
    public string? PushData { get; set; }
}