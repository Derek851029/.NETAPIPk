using Dapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PKApp.ConfigOptions;
using PKApp.DIObject;

namespace PKApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TagsController : ControllerBase
    {
        private readonly DapperContext _context;
        private readonly ILogger<TagsController> _logger;
        public TagsController(DapperContext context, ILogger<TagsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetTags()
        {
            try
            {
                string sql = @"select * from tags where deleted = 0";
                using (var conn = _context.CreateConnection())
                {
                    var tagsData = await conn.QueryAsync(sql);
                    return Ok(tagsData.ToList());
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> NewTag(FrontEndData json)
        {
            try
            {
                string sql = "";
                var dataInfo = JsonConvert.DeserializeObject<TagData>(json.DataInfo);
                dynamic returnData = "";

                using (var conn = _context.CreateConnection())
                {
                    sql = @"insert into tags(deleted, tag_type, top, tag_title, changed, changed_aid, created, created_aid) 
                            values(@deleted, @tag_type, @top, @tag_title, @changed, @changed_aid, @created, @created_aid)";

                    await conn.QueryAsync(sql, new
                    {
                        deleted = 0,
                        tag_type = dataInfo.Tag_type,
                        top = 0,
                        tag_title = dataInfo.Tag_title,
                        changed = 0,
                        changed_aid = 0,
                        created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                        created_aid = HttpContext.Request.Cookies["AccountAid"],
                    });
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

public class TagData
{
    public string? Tag_type { get; set; }
    public string? Tag_title { get; set; }
}
