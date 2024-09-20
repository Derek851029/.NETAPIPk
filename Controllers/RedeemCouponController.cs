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
    public class RedeemCouponController : ControllerBase
    {
        private readonly ILogger<RedeemCouponController> _logger;
        private readonly DapperContext _context;
        private readonly IFilesService _filesService;

        public RedeemCouponController(ILogger<RedeemCouponController> logger, DapperContext context, IFilesService filesService)
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
                            sql = @"SELECT * FROM app_redeem_coupon_bak WHERE deleted = 0 AND status = 1 order by point asc";

                            returnData = await conn.QueryAsync(sql);
                            break;

                        case "edit":
                            sql = @"SELECT * FROM app_redeem_coupon_bak WHERE app_redeem_coupon_id = @app_redeem_coupon_id";

                            returnData = await conn.QueryAsync(sql, new { app_redeem_coupon_id = id });
                            break;

                        case "tags":
                            sql = @"SELECT * FROM app_redeem_coupon_tags WHERE app_redeem_coupon_id = @app_redeem_coupon_id";
                            returnData = await conn.QueryAsync(sql, new { app_redeem_coupon_id = id });
                            break;

                        case "version":
                            sql = @"SELECT * FROM app_redeem_coupon_version WHERE app_redeem_coupon_id = @app_redeem_coupon_id order by ver_created desc";

                            returnData = await conn.QueryAsync(sql, new { app_redeem_coupon_id = id });
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
                var dataInfo = JsonConvert.DeserializeObject<RedeemCouponData>(json.DataInfo);
                dynamic returnData = "Success";

                long todayUnix = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                var userAid = HttpContext.Request.Cookies["AccountAid"];

                using (var conn = _context.CreateConnection())
                {
                    switch (type)
                    {
                        case "new":
                            sql = @"INSERT INTO app_redeem_coupon_bak (version,status,deleted,brand,type,point,sl_coupon_id,sortweight,changed,changed_aid,created,created_aid)
                                    VALUES(@version,@status,@deleted,@brand,@type,@point,@sl_coupon_id,@sortweight,@changed,@changed_aid,@created,@created_aid);";

                            await conn.QueryAsync(sql, new
                            {
                                version = 1,
                                status = dataInfo.Status,
                                deleted = 0,
                                brand = dataInfo.Brand,
                                type = dataInfo.Type,
                                point = dataInfo.Point,
                                sl_coupon_id = dataInfo.Sl_coupon_id,
                                sortweight = 50,
                                changed = 0,
                                changed_aid = 0,
                                created = todayUnix,
                                created_aid = userAid,
                            });
                            break;

                        case "edit":
                            int version = await InsertVersion((int)json.RedeemCouponID);

                            sql = @"UPDATE app_redeem_coupon_bak SET app_redeem_coupon_id = @app_redeem_coupon_id,version = @version,status = @status,brand = @brand,type = @type,point = @point,
                                    sl_coupon_id = @sl_coupon_id,sortweight = @sortweight,changed = @changed,changed_aid = @changed_aid WHERE app_redeem_coupon_id = @app_redeem_coupon_id;";
                            await conn.QueryAsync(sql, new
                            {
                                version = version + 1,
                                status = dataInfo.Status,
                                brand = dataInfo.Brand,
                                type = dataInfo.Type,
                                point = dataInfo.Point,
                                sl_coupon_id = dataInfo.Sl_coupon_id,
                                sortweight = dataInfo.Sortweight,
                                changed = todayUnix,
                                changed_aid = userAid,
                                app_redeem_coupon_id = json.RedeemCouponID,
                            });
                            break;

                        case "tags":
                            string[] tags = new string[1];
                            sql = @"DELETE FROM app_redeem_coupon_tags WHERE app_redeem_coupon_id = @app_redeem_coupon_id";
                            await conn.QueryAsync(sql, new { app_redeem_coupon_id = json.RedeemCouponID });

                            if (dataInfo.TagsData.Any())
                            {
                                tags = dataInfo.TagsData.Split(',');
                                sql = @"INSERT INTO app_redeem_coupon_tags(app_redeem_coupon_id, tag_id)values(@app_redeem_coupon_id, @tagsID)";

                                foreach (string tag in tags)
                                {
                                    returnData = conn.QueryAsync<int>(sql, new
                                    {
                                        app_redeem_coupon_id = json.RedeemCouponID,
                                        tagsID = tag,
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

        public async Task<int> InsertVersion(int id)
        {
            try
            {
                string sql = "";
                List<string> jsonStr = new List<string>();

                using (var conn = _context.CreateConnection())
                {

                    sql = @"INSERT INTO app_redeem_coupon_version(app_redeem_coupon_id,version,status,deleted,brand,point,sl_coupon_id,sortweight,ver_created,ver_created_aid)
                            SELECT app_redeem_coupon_id AS app_redeem_coupon_id, version AS version, status AS status, deleted AS deleted, brand AS brand, point AS point, 
                            sl_coupon_id AS sl_coupon_id, sortweight AS sortweight, @ver_created AS ver_created, @ver_created_aid AS ver_created_aid FROM app_redeem_coupon_bak WHERE app_redeem_coupon_id = @app_redeem_coupon_id";

                    await conn.QueryAsync(sql, new
                    {
                        app_redeem_coupon_id = id,
                        ver_created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                        ver_created_aid = HttpContext.Request.Cookies["AccountAid"],
                    });

                    sql = @"SELECT * FROM app_redeem_coupon_bak WHERE app_redeem_coupon_id = @app_redeem_coupon_id";

                    var getVersion = await conn.QuerySingleOrDefaultAsync(sql, new { app_redeem_coupon_id = id });

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

public class RedeemCouponData
{
    public int? Status { get; set; }
    public string? Brand { get; set; }
    public string? Type { get; set; }
    public int? Point { get; set; }
    public string? Sl_coupon_id { get; set; }
    public int? Sortweight { get; set; }
    public string? TagsData { get; set; }
}
