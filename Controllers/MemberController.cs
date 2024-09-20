using Dapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PKApp.ConfigOptions;
using PKApp.DIObject;

namespace PKApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MemberController : ControllerBase
    {
        private readonly DapperContext _context;
        private readonly ILogger _logger;

        public MemberController(DapperContext context, ILogger<MemberController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetMemberData(string type, int current, int id)
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
                            sql =
                                id == 0
                                ? @"SELECT *, (SELECT COUNT(*) FROM member) AS TotalCount FROM member LIMIT 10 OFFSET @count"
                                : "SELECT *, (SELECT COUNT(*) FROM member  WHERE pk_id LIKE @pk_id) AS TotalCount FROM member WHERE pk_id LIKE @pk_id LIMIT 10 OFFSET @count";
                            returnData =
                                id == 0 ? await conn.QueryAsync(sql, new { count = current * 10 })
                                : await conn.QueryAsync(sql, new { count = current * 10, pk_id = "%" + id + "%" });
                            break;

                        case "edit":
                            sql = @"SELECT * FROM member WHERE mid = @mid";

                            returnData = await conn.QueryAsync(sql, new { mid = id });
                            break;
                        case "tags":
                            sql = @"SELECT * FROM member_tags WHERE mid = @mid";

                            returnData = await conn.QueryAsync(sql, new { mid = id });
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
                var dataInfo = JsonConvert.DeserializeObject<MemberData>(json.DataInfo);
                dynamic returnData = "Success";

                long todayUnix = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                var userAid = HttpContext.Request.Cookies["AccountAid"];

                using (var conn = _context.CreateConnection())
                {
                    switch (type)
                    {
                        case "tags":
                            string[] tags = new string[1];
                            sql = @"delete from member_tags where mid = @mid";
                            await conn.QueryAsync(sql, new { mid = json.MemberID });

                            if (dataInfo.TagsData.Any())
                            {
                                tags = dataInfo.TagsData.Split(',');
                                sql = @"INSERT INTO member_tags(mid, tag_id,created)values(@mid, @tagsID, @created)";

                                foreach (string tag in tags)
                                {
                                    returnData = conn.QueryAsync<int>(sql, new
                                    {
                                        mid = json.MemberID,
                                        tagsID = tag,
                                        created = todayUnix,
                                    });
                                }
                            }
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

public class MemberData
{
    public string? TagsData { get; set; }
}
