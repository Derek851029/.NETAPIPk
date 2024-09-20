using Dapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PKApp.ConfigOptions;
using PKApp.DIObject;

namespace PKApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppSettingController : ControllerBase
    {
        private readonly ILogger<AppSettingController> _logger;
        private readonly DapperContext _context;

        public AppSettingController(ILogger<AppSettingController> logger, DapperContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string type, string id)
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
                            sql = @"SELECT * FROM app_setting";

                            returnData = await conn.QueryAsync(sql);
                            break;

                        case "edit":
                            if (id == "setting_url")
                            {
                                sql = @"SELECT * FROM app_setting WHERE setting_title LIKE '%肯德基%' OR setting_title LIKE '%必勝客%' OR setting_title LIKE '%線上客服%'";
                            }
                            else if (id == "strolletStatus")
                            {
                                sql = @"SELECT * FROM app_strollet_status";
                            }
                            else
                            {
                                sql = @"SELECT * FROM app_setting WHERE setting_key = @setting_key";
                            }


                            returnData = await conn.QueryAsync(sql, new { setting_key = id });
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
                dynamic returnData = "Success";

                long todayUnix = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                var userAid = HttpContext.Request.Cookies["AccountAid"];

                using (var conn = _context.CreateConnection())
                {
                    sql = @"UPDATE app_setting SET setting_value = @setting_value, changed = @changed,  changed_aid = @changed_aid  
                                    WHERE setting_key = @setting_key";
                    switch (json.SettingKey)
                    {
                        case "setting_url":
                            JObject jsonObject = JObject.Parse(json.DataInfo);
                            foreach (var property in jsonObject.Properties())
                            {
                                await conn.QueryAsync(sql, new
                                {
                                    setting_value = property.Value.ToString(),
                                    setting_key = property.Name,
                                    changed = todayUnix,
                                    changed_aid = userAid
                                });
                            }

                            break;

                        case "win_invoices_terms":
                        case "membership_terms":
                        case "e_invoices_terms":
                        case "e_red_voucher_terms":
                            await conn.QueryAsync(sql, new
                            {
                                setting_value = json.DataInfo,
                                setting_key = json.SettingKey,
                                changed = todayUnix,
                                changed_aid = userAid
                            });
                            break;
                        case "strolletStatus":
                            sql = @"UPDATE app_strollet_status SET status = @status, message = @message";
                            var stValue = JsonConvert.DeserializeObject<AppSettingData>(json.DataInfo);
                            await conn.QueryAsync(sql, new
                            {
                                status = stValue.Status,
                                message = stValue.Message,
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

public class AppSettingData
{
    public string? chatbot { get; set; }
    public string? delivery_kfc { get; set; }
    public string? online_order_kfc { get; set; }
    public string? online_order_pizzahut { get; set; }
    public string? order_card_kfc { get; set; }
    public string? order_card_pizzahut { get; set; }
    public string? order_history_kfc { get; set; }
    public string? order_history_pizzahut { get; set; }
    public string? order_setting_kfc { get; set; }
    public string? order_setting_pizzahut { get; set; }
    public string? redeem_commodity_kfc { get; set; }
    public string? redeem_commodity_pizzahut { get; set; }
    public string? redeem_coupon_kfc { get; set; }
    public string? redeem_coupon_pizzahut { get; set; }
    public string? redeem_offer_kfc { get; set; }
    public string? redeem_offer_pizzahut { get; set; }
    public string? store_kfc { get; set; }
    public string? store_pizzahut { get; set; }
    public int? Status { get; set; }
    public string? Message { get; set; }
}
