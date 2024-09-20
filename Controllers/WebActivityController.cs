using Dapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PKApp.ConfigOptions;
using PKApp.DIObject;
using PKApp.Services;

namespace PKApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebActivityController : Controller
    {
        private readonly IWebActivityService _webActivityService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WebActivityController> _logger;
        private readonly DapperContext _context;
        private readonly IFilesService _filesService;
        private readonly ICrypto _crypto;

        public WebActivityController(IWebActivityService webActivityService,
            IConfiguration configuration,
            ILogger<WebActivityController> logger,
            DapperContext context,
            IFilesService filesService,
            ICrypto crypto)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
            _filesService = filesService;
            _crypto = crypto;
            _webActivityService = webActivityService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string type, string activity, int version, int id)
        {
            try
            {
                string sql = "";
                string dbName = activity == "DrawPrize" ? "activity_drawprize" :
                    activity == "Quiz" ? "activity_quiz" :
                    activity == "Slots" ? "activity_slots" : "";

                string prizeDBName = activity == "DrawPrize" ? "activity_drawprize_prize" :
                    activity == "Quiz" ? "activity_quiz_prize" :
                    activity == "Slots" ? "activity_slots_prize" : "";

                IEnumerable<dynamic> returnData = null;

                using (var conn = _context.CreateConnection2())
                {
                    switch (type)
                    {
                        case "list":
                            sql = @"SELECT * FROM activity WHERE activity_type = @activity_type AND deleted = 0 order by created desc";

                            returnData = await conn.QueryAsync(sql, new
                            {
                                activity_type = activity
                            });
                            break;
                        case "edit":
                            sql = @"SELECT * FROM activity WHERE activity_id = @activity_id";
                            var activityData = await conn.QueryAsync(sql, new { activity_id = id });

                            sql = $"SELECT * FROM {dbName} WHERE activity_id = @activity_id";
                            var activityInfo = await conn.QueryAsync(sql, new { activity_id = id });

                            sql = $"SELECT * FROM {prizeDBName} WHERE activity_id = @activity_id";
                            var prize = await conn.QueryAsync(sql, new { activity_id = id });

                            if (activity == "Quiz")
                            {
                                sql = @"SELECT * FROM activity_quiz_question WHERE activity_id = @activity_id";
                                var question = await conn.QueryAsync(sql, new { activity_id = id });
                                return Ok(new
                                {
                                    activityData = activityData,
                                    activityInfo = activityInfo,
                                    prize = prize,
                                    question = question,
                                });
                            }

                            return Ok(new
                            {
                                activityData = activityData,
                                activityInfo = activityInfo,
                                prize = prize,
                            });
                        case "demo":
                            string url = await CreateDemoUrl(id);
                            return Ok(url);
                        case "amount":
                            sql = $"SELECT * FROM {prizeDBName} WHERE activity_id = @activity_id";
                            returnData = await conn.QueryAsync(sql, new { activity_id = id });
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
                JObject dataInfo = JsonConvert.DeserializeObject<JObject>(json.DataInfo);
                dynamic returnData = "Success";
                long today = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                int icon_fid = 0;

                int id = json.WebActivityID ?? 0;
                using (var conn = _context.CreateConnection2())
                {
                    switch (type)
                    {
                        case "new":
                        case "edit":
                            await _webActivityService.HandleDB(type, dataInfo, id);
                            break;
                        case "status":
                            sql = @"UPDATE activity SET status = @status WHERE activity_id = @activity_id";
                            await conn.QueryAsync(sql, new { status = (string)dataInfo["Status"], activity_id = json.WebActivityID });
                            break;
                        case "delete":
                            sql = @"UPDATE activity SET deleted = 1 WHERE activity_id = @activity_id";
                            await conn.QueryAsync(sql, new { activity_id = json.WebActivityID });
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

        public async Task<string> CreateDemoUrl(int activityId)
        {
            string? env = Environment.GetEnvironmentVariable("stage");
            string gameUrl = _configuration[$"GameUrl_{env}"];

            DateTimeOffset now = DateTimeOffset.Now;
            Int32 zeroTimestamp = (Int32)now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();

            Random random = new Random();
            int randomNumber = random.Next(12, 17);
            string token = new string(Enumerable.Repeat("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", randomNumber)
            .Select(s => s[new Random().Next(s.Length)]).ToArray());

            string concatStr = $"{activityId}.{token}.2661976.{zeroTimestamp}";
            string encrypt = await _crypto.Encrypt(concatStr);
            string urlToken = Uri.EscapeDataString(encrypt);
            return $"{gameUrl}/appgame/{activityId}?t={urlToken}";
        }
    }
}
