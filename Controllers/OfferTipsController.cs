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
    public class OfferTipsController : ControllerBase
    {
        private readonly ILogger<OfferTipsController> _logger;
        private readonly DapperContext _context;
        private readonly IFilesService _filesService;

        public OfferTipsController(ILogger<OfferTipsController> logger, DapperContext context, IFilesService filesService)
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
                            sql = @"SELECT * FROM app_offertips WHERE deleted = 0 order by point asc";

                            returnData = await conn.QueryAsync(sql);
                            break;

                        case "edit":
                            sql = @"SELECT * FROM app_offertips WHERE app_offertips_id = @app_offertips_id";

                            returnData = await conn.QueryAsync(sql, new { app_offertips_id = id });
                            break;

                        case "tags":
                            sql = @"SELECT * FROM app_offertips_tags WHERE app_offertips_id = @app_offertips_id";
                            returnData = await conn.QueryAsync(sql, new { app_offertips_id = id });
                            break;

                        case "version":
                            sql = @"SELECT * FROM app_offertips_version WHERE app_offertips_id = @app_offertips_id order by ver_created desc";

                            returnData = await conn.QueryAsync(sql, new { app_offertips_id = id });
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
                var dataInfo = JsonConvert.DeserializeObject<OfferTipsData>(json.DataInfo);
                dynamic returnData = "Success";

                long todayUnix = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                var userAid = HttpContext.Request.Cookies["AccountAid"];

                using (var conn = _context.CreateConnection())
                {
                    switch (type)
                    {
                        case "new":
                            sql = @"INSERT INTO app_offertips (version,status,deleted,point,tips,coming_soon_tips,click_type,click_value,
                                    changed,changed_aid,created,created_aid)
                                    VALUES(@version,@status,@deleted,@point,@tips,@coming_soon_tips,@click_type,@click_value,@changed,
                                    @changed_aid,@created,@created_aid);";

                            await conn.QueryAsync(sql, new
                            {
                                version = 1,
                                status = 1,
                                deleted = 0,
                                point = dataInfo.Point,
                                tips = dataInfo.Tips,
                                coming_soon_tips = dataInfo.Coming_soon_tips,
                                click_type = dataInfo.Click_type,
                                click_value = dataInfo.Click_value,
                                changed = 0,
                                changed_aid = 0,
                                created = todayUnix,
                                created_aid = userAid,
                            });
                            break;

                        case "edit":
                            int version = await InsertVersion((int)json.OfferTipsID);

                            sql = @"UPDATE app_offertips SET version = @version, point = @point,tips = @tips,coming_soon_tips = @coming_soon_tips,click_type = @click_type,click_value = @click_value,
                                    changed = @changed,changed_aid = @changed_aid WHERE app_offertips_id = @app_offertips_id;";
                            await conn.QueryAsync(sql, new
                            {
                                version = version + 1,
                                point = dataInfo.Point,
                                tips = dataInfo.Tips,
                                coming_soon_tips = dataInfo.Coming_soon_tips,
                                click_type = dataInfo.Click_type,
                                click_value = dataInfo.Click_value,
                                changed = todayUnix,
                                changed_aid = userAid,
                                app_offertips_id = json.OfferTipsID,
                            });
                            break;

                        case "tags":
                            string[] tags = new string[1];
                            sql = @"DELETE FROM app_offertips_tags WHERE app_offertips_id = @app_offertips_id";
                            await conn.QueryAsync(sql, new { app_offertips_id = json.OfferTipsID });

                            if (dataInfo.TagsData.Any())
                            {
                                tags = dataInfo.TagsData.Split(',');
                                sql = @"INSERT INTO app_offertips_tags(app_offertips_id, tag_id)values(@app_offertips_id, @tagsID)";

                                foreach (string tag in tags)
                                {
                                    returnData = conn.QueryAsync<int>(sql, new
                                    {
                                        app_offertips_id = json.ActivityID,
                                        tagsID = tag,
                                    });
                                }
                            }

                            break;

                        case "delete":
                            sql = @"UPDATE app_offertips SET deleted = 1 WHERE app_offertips_id = @app_offertips_id";
                            await conn.QueryAsync(sql, new { app_offertips_id = json.OfferTipsID });
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

                    sql = @"INSERT INTO app_offertips_version(app_offertips_id,version,status,deleted,point,tips,coming_soon_tips,click_type,
                            click_value,ver_created,ver_created_aid)
                            SELECT app_offertips_id AS app_offertips_id, version AS version, status AS status, deleted AS deleted, point AS point, 
                            tips AS tips, coming_soon_tips AS coming_soon_tips, click_type AS click_type, click_value AS click_value, 
                            @ver_created AS ver_created, @ver_created_aid AS ver_created_aid FROM app_offertips WHERE app_offertips_id = @app_offertips_id";

                    await conn.QueryAsync(sql, new
                    {
                        app_offertips_id = id,
                        ver_created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                        ver_created_aid = HttpContext.Request.Cookies["AccountAid"],
                    });

                    sql = @"SELECT * FROM app_offertips WHERE app_offertips_id = @app_offertips_id";

                    var getVersion = await conn.QuerySingleOrDefaultAsync(sql, new { app_offertips_id = id });

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

public class OfferTipsData
{
    public int? Point { get; set; }
    public string? Tips { get; set; }
    public string? Coming_soon_tips { get; set; }
    public string? Click_type { get; set; }
    public string? Click_value { get; set; }
    public string? TagsData { get; set; }
}
