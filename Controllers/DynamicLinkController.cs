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
    public class DynamicLinkController : ControllerBase
    {
        private readonly ILogger<DynamicLinkController> _logger;
        private readonly DapperContext _context;
        private readonly IFilesService _filesService;
        public DynamicLinkController(ILogger<DynamicLinkController> logger, DapperContext context, IFilesService filesService)
        {
            _logger = logger;
            _context = context;
            _filesService = filesService;
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
                            sql = @"SELECT * FROM dynamic_link order by created desc";

                            returnData = await conn.QueryAsync(sql);
                            break;

                        case "edit":
                            sql = @"SELECT * FROM dynamic_link WHERE dynamic_link_id = @dynamic_link_id";

                            returnData = await conn.QueryAsync(sql, new { dynamic_link_id = id });
                            break;

                        case "version":
                            sql = @"SELECT * FROM dynamic_link_version WHERE dynamic_link_id = @dynamic_link_id order by version_date desc";

                            returnData = await conn.QueryAsync(sql, new { dynamic_link_id = id });
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
        public async Task<IActionResult> NewOrGetData(FrontEndData json)
        {
            try
            {
                string sql = "";
                string type = json.Type;
                var dataInfo = JsonConvert.DeserializeObject<ActivityData>(json.DataInfo);
                dynamic returnData = "Success";

                int image_fid = 0;
                int icon_fid = 0;

                using (var conn = _context.CreateConnection())
                {
                    switch (type)
                    {
                        case "new":
                            string randomString;
                            bool isDuplicate;
                            do
                            {
                                randomString = GenerateRandomString();
                                isDuplicate = CheckExists(randomString);
                            } while (isDuplicate);

                            sql = @"INSE";
                            break;

                        case "edit":
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

        public string GenerateRandomString()
        {
            string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var result = new string(Enumerable.Repeat(chars, 16)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            return result;
        }

        public bool CheckExists(string value)
        {
            using (var conn = _context.CreateConnection())
            {
                string sql = @"SELECT COUNT(*) FROM dynamic_link WHERE rand_key = @rand_key";

                var count = conn.Query<int>(sql, new { rand_key = value });

                return Convert.ToInt32(count) > 0;
            }
        }
    }
}

public class DynamicLinkData
{
    public string? Source { get; set; }
    public string? Action { get; set; }
    public int? action_id { get; set; }
}
