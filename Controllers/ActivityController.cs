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
    public class ActivityController : ControllerBase
    {
        private readonly ILogger<ActivityController> _logger;
        private readonly DapperContext _context;
        private readonly IFilesService _filesService;
        public ActivityController(ILogger<ActivityController> logger, DapperContext context, IFilesService filesService)
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
                            sql = @"SELECT * FROM app_activity WHERE deleted = 0 order by created desc";

                            returnData = await conn.QueryAsync(sql);
                            break;

                        case "edit":
                        case "view":
                            sql = @"SELECT * FROM app_activity WHERE app_activity_id = @app_activity_id";

                            returnData = await conn.QueryAsync(sql, new { app_activity_id = id });
                            break;
                        case "giftList":
                            sql = @"SELECT * FROM app_activity activity LEFT JOIN app_activity_prize prize ON activity.app_activity_id = prize.app_activity_id 
                                    WHERE prize.app_activity_id = @app_activity_id";
                            returnData = await conn.QueryAsync(sql, new { app_activity_id = id });
                            break;
                        case "qList":
                            sql = @"SELECT * FROM app_activity activity LEFT JOIN activity_question_item item ON activity.app_activity_id = item.actq_id 
                                    WHERE item.actq_id = @app_activity_id";
                            returnData = await conn.QueryAsync(sql, new { app_activity_id = id });
                            break;
                        case "giftData":
                            sql = @"SELECT * FROM app_activity_prize WHERE app_activity_prize_id = @app_activity_prize_id";
                            returnData = await conn.QueryAsync(sql, new { app_activity_prize_id = id });
                            break;
                        case "qData":
                            sql = @"SELECT * FROM activity_question_item WHERE actq_item_id = @actq_item_id";
                            returnData = await conn.QueryAsync(sql, new { actq_item_id = id });
                            break;
                        case "tags":
                            sql = @"select * FROM app_activity_tags where app_activity_id = @app_activity_id";

                            returnData = await conn.QueryAsync(sql, new { app_activity_id = id });
                            break;
                        case "version":
                            sql = @"SELECT * FROM app_activity_version where app_activity_id = @app_activity_id";
                            returnData = await conn.QueryAsync(sql, new { app_activity_id = id });
                            break;
                        case "versionView":
                            sql = @"SELECT * FROM app_activity_version where app_activity_id = @app_activity_id AND version = @version";
                            returnData = await conn.QueryAsync(sql, new { app_activity_id = id, version = version });
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
                            if (dataInfo.App_image != null) { icon_fid = await _filesService.UploadFilesToGCS(dataInfo, "icon", "activity"); }

                            sql = @"INSERT INTO app_activity (version,status,deleted,activity_type,title,description,cooldown_text,not_winning_text,
                                    image_fid,start_time,end_time,play_interval,chance_of_winning,actq_id,push_title,icon_fid,changed,changed_aid,
                                    created,created_aid,is_free)
                                    VALUES(@version,@status,@deleted,@activity_type,@title,@description,@cooldown_text,
                                    @not_winning_text,@image_fid,@start_time,@end_time,@play_interval,@chance_of_winning,@actq_id,
                                    @push_title,@icon_fid,@changed,@changed_aid,@created,@created_aid,@is_free);";
                            returnData = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                            {
                                version = 1,
                                status = 1,
                                deleted = 0,
                                activity_type = dataInfo.Activity_type,
                                title = dataInfo.Title,
                                description = dataInfo.Description,
                                cooldown_text = dataInfo.Cooldown_text,
                                not_winning_text = dataInfo.Not_winning_text,
                                image_fid = image_fid,
                                start_time = DateTimeOffset.Parse(dataInfo.Time[0]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                end_time = DateTimeOffset.Parse(dataInfo.Time[1]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                play_interval = dataInfo.Play_interval,
                                chance_of_winning = dataInfo.Chance_of_winning,
                                actq_id = 0,
                                push_title = dataInfo.Push_title,
                                icon_fid = icon_fid,
                                changed = 0,
                                changed_aid = 0,
                                created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                created_aid = HttpContext.Request.Cookies["AccountAid"],
                                is_free = dataInfo.is_free,
                            });
                            break;

                        case "newGift":
                            sql = @"INSERT INTO app_activity_prize (app_activity_id, prize_group, sl_coupon_id, brand, amount, take_amount) 
                                    VALUES(@app_activity_id, @prize_group, @sl_coupon_id, @brand, @amount, @take_amount)";

                            await conn.QueryAsync(sql, new
                            {
                                app_activity_id = json.ActivityID,
                                prize_group = dataInfo.Prize_group.Any() ? dataInfo.Prize_group : "",
                                sl_coupon_id = dataInfo.Sl_coupon_id,
                                brand = dataInfo.Brand,
                                amount = dataInfo.Amount,
                                take_amount = 0,
                            });
                            break;

                        case "newQBank":
                            sql = @"INSERT INTO activity_question_item (actq_id, subject, option_a, reply_a, option_b, reply_b) 
                                    VALUES(@actq_id, @subject, @option_a, @reply_a, @option_b, @reply_b);

                                    SELECT CAST(LAST_INSERT_ID() AS UNSIGNED INTEGER)";

                            var actq_item_id = await conn.QueryAsync(sql, new
                            {
                                actq_id = json.ActivityID,
                                subject = dataInfo.Subject,
                                option_a = dataInfo.Option_a,
                                reply_a = dataInfo.Reply_a,
                                option_b = dataInfo.Option_b,
                                reply_b = dataInfo.Reply_b,
                            });
                            break;

                        case "edit":
                            if (dataInfo.App_image != null) { icon_fid = await _filesService.UploadFilesToGCS(dataInfo, "icon", "activity"); }

                            if (icon_fid == 0)
                            {
                                sql = @"select icon_fid from app_activity where app_activity_id = @activityID";
                                icon_fid = await conn.QuerySingleOrDefaultAsync<int>(sql, new { activityID = json.ActivityID });
                            }

                            int version = await InsertVersion((int)json.ActivityID);

                            sql = @"UPDATE app_activity SET version = @version, activity_type = @activity_type,title = @title,description = @description,cooldown_text = @cooldown_text,
                                    not_winning_text = @not_winning_text,start_time = @start_time,end_time = @end_time,play_interval = @play_interval,
                                    chance_of_winning = @chance_of_winning,push_title = @push_title,icon_fid = @icon_fid,changed = @changed,
                                    changed_aid = @changed_aid,is_free = @is_free WHERE app_activity_id = @app_activity_id";

                            await conn.QueryAsync(sql, new
                            {
                                version = version + 1,
                                activity_type = dataInfo.Activity_type,
                                title = dataInfo.Title,
                                description = dataInfo.Description,
                                cooldown_text = dataInfo.Cooldown_text,
                                not_winning_text = dataInfo.Not_winning_text,
                                start_time = DateTimeOffset.Parse(dataInfo.Time[0]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                end_time = DateTimeOffset.Parse(dataInfo.Time[1]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                play_interval = dataInfo.Play_interval,
                                chance_of_winning = dataInfo.Chance_of_winning,
                                push_title = dataInfo.Push_title,
                                icon_fid = icon_fid,
                                changed = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                changed_aid = HttpContext.Request.Cookies["AccountAid"],
                                is_free = dataInfo.is_free,
                                app_activity_id = json.ActivityID
                            });
                            break;

                        case "editGift":
                            sql = @"UPDATE app_activity_prize SET sl_coupon_id = @sl_coupon_id, brand = @brand, amount = @amount
                                    WHERE app_activity_prize_id = @app_activity_prize_id";

                            await conn.QueryAsync(sql, new
                            {
                                app_activity_prize_id = json.ActivityPrizeId,
                                sl_coupon_id = dataInfo.Sl_coupon_id,
                                brand = dataInfo.Brand,
                                amount = dataInfo.Amount,
                            });
                            break;

                        case "editQBank":
                            sql = @"UPDATE activity_question_item SET subject = @subject, option_a = @option_a, reply_a = @reply_a,
                                    option_b = @option_b, reply_b = @reply_b WHERE actq_item_id = @actq_item_id";

                            await conn.QueryAsync(sql, new
                            {
                                actq_item_id = json.ActivityQBankId,
                                subject = dataInfo.Subject,
                                option_a = dataInfo.Option_a,
                                reply_a = dataInfo.Reply_a,
                                option_b = dataInfo.Option_b,
                                reply_b = dataInfo?.Reply_b,
                            });
                            break;

                        case "status":
                            sql = @"update app_activity set status = @status where app_activity_id = @activityID";
                            returnData = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                            {
                                status = dataInfo.Status,
                                activityID = json.ActivityID,
                            });
                            break;

                        case "tags":
                            string[] tags = new string[1];
                            sql = @"delete from app_activity_tags where app_activity_id = @app_activity_id";
                            await conn.QueryAsync(sql, new { app_activity_id = json.ActivityID });

                            if (dataInfo.TagsData.Any())
                            {
                                tags = dataInfo.TagsData.Split(',');
                                sql = @"insert into app_activity_tags(app_activity_id, tag_id)values(@app_activity_id, @tagsID)";

                                foreach (string tag in tags)
                                {
                                    returnData = conn.QueryAsync<int>(sql, new
                                    {
                                        app_activity_id = json.ActivityID,
                                        tagsID = tag,
                                    });
                                }
                            }

                            break;

                        case "delete":
                            sql = @"update app_activity set deleted = @status where app_activity_id = @activityID";
                            returnData = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                            {
                                status = dataInfo.Status,
                                activityID = json.ActivityID,
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

        [HttpDelete]
        public async Task<IActionResult> DeleteGift(FrontEndData json)
        {
            try
            {
                dynamic returnData = "Success";
                string sql = "";

                using (var conn = _context.CreateConnection())
                {
                    switch (json.Type)
                    {
                        case "gift":
                            sql = @"DELETE app_activity_prize WHERE app_activity_prize_id = @app_activity_prize_id";
                            await conn.QueryAsync(sql, new { app_activity_prize_id = json.ActivityPrizeId });
                            break;

                        case "qbank":
                            sql = @"DELETE app_activity_prize WHERE actq_item_id = @actq_item_id";
                            await conn.QueryAsync(sql, new { actq_item_id = json.ActivityQBankId });
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
                    //先獲取prize, 並轉成 json array
                    sql = @"SELECT * FROM app_activity_prize WHERE app_activity_id = @app_activity_id";
                    var prizeData = await conn.QueryAsync(sql, new { app_activity_id = id });
                    prizeData.ToList().ForEach(x =>
                    {
                        var json = new
                        {
                            app_activity_prize_id = x.app_activity_prize_id,
                            prize_group = x.prize_group,
                            sl_coupon_id = x.sl_coupon_id,
                            brand = x.brand,
                            amount = x.amount,
                            take_amount = x.take_amount,
                        };
                        jsonStr.Add(JsonConvert.SerializeObject(json));
                    });

                    sql = @"INSERT INTO app_activity_version(app_activity_id,version,status,deleted,activity_type,title,description,cooldown_text,
                            not_winning_text,image_fid,start_time,end_time,play_interval,chance_of_winning, actq_id,push_title,icon_fid,prize_json,ver_created,ver_created_aid)
                            SELECT app_activity_id AS app_activity_id, version AS version, status AS status, deleted AS deleted, activity_type AS activity_type, 
                            title AS title, description AS description, cooldown_text AS cooldown_text, not_winning_text AS not_winning_text, 
                            image_fid AS image_fid, start_time AS start_time, end_time AS end_time,play_interval AS play_interval, 
                            chance_of_winning AS chance_of_winning, actq_id AS actq_id, push_title AS push_title, icon_fid AS icon_fid,@prize_json AS prize_json, @ver_created AS ver_created, 
                            @ver_created_aid AS ver_created_aid FROM app_activity 
                            WHERE app_activity_id = @app_activity_id";

                    await conn.QueryAsync(sql, new
                    {
                        prize_json = "[" + string.Join(",", jsonStr.ToArray()) + "]",
                        app_activity_id = id,
                        ver_created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                        ver_created_aid = HttpContext.Request.Cookies["AccountAid"],
                    });

                    sql = @"SELECT * FROM app_activity WHERE app_activity_id = @app_activity_id ORDER BY version desc";

                    var getVersion = await conn.QuerySingleOrDefaultAsync(sql, new { app_activity_id = id });

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

public class ActivityData
{
    public string? Activity_type { get; set; }
    public string? Title { get; set; }
    public string[]? Time { get; set; }
    public string? Play_interval { get; set; }
    public string? Description { get; set; }
    public string? Cooldown_text { get; set; }
    public string? Not_winning_text { get; set; }
    public string? Push_title { get; set; }
    public string? App_image { get; set; }
    public int? Chance_of_winning { get; set; }
    public int? is_free { get; set; }
    public string? CreateDate { get; set; }
    public int? Status { get; set; }

    public string? TagsData { get; set; }

    //獎品
    public string? Brand { get; set; }
    public string? Prize_group { get; set; }
    public string? Sl_coupon_id { get; set; }
    public int? Amount { get; set; }

    //題庫
    public string? Subject { get; set; }
    public string? Option_a { get; set; }
    public string? Reply_a { get; set; }
    public string? Option_b { get; set; }
    public string? Reply_b { get; set; }
}
