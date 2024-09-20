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
    public class MessageController : ControllerBase
    {
        private readonly ILogger<NewsController> _logger;
        private readonly DapperContext _context;
        private readonly IFilesService _filesService;

        public MessageController(ILogger<NewsController> logger, DapperContext context, IFilesService filesService)
        {
            _logger = logger;
            _context = context;
            _filesService = filesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMessage(string type, int id, int version)
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
                            sql = @"select * from app_message where deleted = 0 order by created desc";

                            returnData = await conn.QueryAsync(sql);
                            break;
                        case "edit":
                            sql = @"select * from app_message where app_message_id = @app_message_id";
                            returnData = await conn.QueryAsync(sql, new
                            {
                                app_message_id = id,
                            });
                            break;
                        case "editVersion":
                            sql = @"SELECT * FROM app_message_version WHERE app_message_id = @app_message_id and version = @version";
                            returnData = await conn.QueryAsync(sql, new
                            {
                                app_message_id = id,
                                version = version,
                            });
                            break;
                        case "tags":
                            sql = @"select * from app_message_tags where app_message_id = @app_message_id";
                            returnData = await conn.QueryAsync(sql, new
                            {
                                app_message_id = id,
                            });
                            break;
                        case "version":
                            sql = @"select * from app_message_version where app_message_id = @app_message_id";
                            returnData = await conn.QueryAsync(sql, new
                            {
                                app_message_id = id,
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

        [HttpPost]
        public async Task<IActionResult> NewMessage(FrontEndData json)
        {
            try
            {
                string sql = "";
                string type = json.Type;
                var dataInfo = JsonConvert.DeserializeObject<MessageData>(json.DataInfo);
                dynamic returnData = "Success";
                int image_fid = 0;
                int icon_fid = 0;
                int cover_fid = 0;
                using (var conn = _context.CreateConnection())
                {
                    switch (type)
                    {
                        case "new":
                            if (dataInfo.Activity_image != null) { image_fid = await _filesService.UploadFilesToGCS(dataInfo, "image", "app_message"); }
                            if (dataInfo.App_image != null) { icon_fid = await _filesService.UploadFilesToGCS(dataInfo, "icon", "app_message"); }
                            if (dataInfo.Cover_image != null) { cover_fid = await _filesService.UploadFilesToGCS(dataInfo, "cover", "app_message"); }

                            sql = @"insert into app_message (version,status,deleted,archive_time,type,brand,show_time,title,content,icon_fid,
                                    image_fid,cover_image_fid,video_fid,click_title,click_type,click_value,start_time,keep_time,end_time,changed,
                                    changed_origin,changed_aid,created,created_origin,created_aid) values(@version,@status,@deleted,@archive_time,
                                    @type,@brand,@show_time,@title,@content,@icon_fid,@image_fid,@cover_image_fid,@video_fid,@click_title,@click_type,
                                    @click_value,@start_time,@keep_time,@end_time,@changed,@changed_origin,@changed_aid,@created,@created_origin,@created_aid)";

                            await conn.QueryAsync(sql, new
                            {
                                version = 1,
                                status = dataInfo.Status,
                                deleted = 0,
                                archive_time = 0,
                                type = dataInfo.Type,
                                brand = dataInfo.Brand,
                                show_time = 0,
                                title = dataInfo.Title,
                                content = dataInfo.Content,
                                icon_fid = icon_fid,
                                image_fid = image_fid,
                                cover_image_fid = cover_fid,
                                video_fid = 0,
                                click_title = dataInfo.Click_title,
                                click_type = dataInfo.Click_type,
                                click_value = dataInfo.Click_value,
                                start_time = 0,
                                keep_time = dataInfo.Keep_time,
                                end_time = 0,
                                changed = 0,
                                changed_origin = "",
                                changed_aid = 0,
                                created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                created_origin = "Web",
                                created_aid = HttpContext.Request.Cookies["AccountAid"],
                            });
                            break;

                        case "edit":
                            int originalVersion = await InsertVersion(json.OriginalData);

                            if (dataInfo.Activity_image != null) { image_fid = await _filesService.UploadFilesToGCS(dataInfo, "image", "app_message"); }
                            if (dataInfo.App_image != null) { icon_fid = await _filesService.UploadFilesToGCS(dataInfo, "icon", "app_message"); }
                            if (dataInfo.Cover_image != null) { cover_fid = await _filesService.UploadFilesToGCS(dataInfo, "cover", "app_message"); }

                            if (image_fid == 0)
                            {
                                sql = @"select image_fid from app_message where app_message_id = @app_message_id";
                                image_fid = await conn.QuerySingleOrDefaultAsync<int>(sql, new { app_message_id = json.MessageID });
                            }

                            if (icon_fid == 0)
                            {
                                sql = @"select icon_fid from app_message where app_message_id = @app_message_id";
                                icon_fid = await conn.QuerySingleOrDefaultAsync<int>(sql, new { app_message_id = json.MessageID });
                            }

                            //if (cover_fid == 0)
                            //{
                            //    sql = @"select cover_image_fid from app_message where app_message_id = @app_message_id";
                            //    cover_fid = await conn.QuerySingleOrDefaultAsync<int>(sql, new { app_message_id = json.MessageID });
                            //}

                            sql = @"update app_message set version = @version, status = @status, type = @type, brand = @brand, show_time = @show_time,
                                    title = @title, content = @content, icon_fid = @icon_fid, image_fid = @image_fid, cover_image_fid = @cover_image_fid, click_title = @click_title,
                                    click_type = @click_type, click_value = @click_value, start_time = @start_time, keep_time = @keep_time, end_time = @end_time, 
                                    changed = @changed, changed_origin = @changed_origin, changed_aid = @changed_aid where app_message_id = @app_message_id";

                            await conn.QueryAsync(sql, new
                            {
                                version = originalVersion + 1,
                                status = dataInfo.Status,
                                type = dataInfo.Type,
                                brand = dataInfo.Brand,
                                show_time = 0,
                                title = dataInfo.Title,
                                content = dataInfo.Content,
                                icon_fid = icon_fid,
                                image_fid = image_fid,
                                cover_image_fid = cover_fid,
                                click_title = dataInfo.Click_title,
                                click_type = dataInfo.Click_type,
                                click_value = dataInfo.Click_value,
                                start_time = 0,
                                keep_time = dataInfo.Keep_time,
                                end_time = 0,
                                changed = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                changed_origin = "Web",
                                changed_aid = HttpContext.Request.Cookies["AccountAid"],
                                app_message_id = json.MessageID,
                            });
                            break;

                        case "delete":
                            sql = @"update app_message set deleted = @status where app_message_id = @app_message_id";
                            returnData = await conn.QueryAsync(sql, new
                            {
                                status = dataInfo.Status,
                                app_message_id = json.MessageID,
                            });
                            break;

                        case "tags":
                            string[] tags = new string[1];
                            sql = @"delete from app_message_tags where app_message_id = @app_message_id";
                            await conn.QueryAsync(sql, new { app_message_id = json.MessageID });

                            if (dataInfo.TagsData.Any())
                            {
                                tags = dataInfo.TagsData.Split(',');
                                sql = @"insert into app_message_tags(app_message_id, tag_id)values(@app_message_id, @tagsID)";

                                foreach (string tag in tags)
                                {
                                    returnData = conn.QueryAsync<int>(sql, new
                                    {
                                        app_message_id = json.MessageID,
                                        tagsID = tag,
                                    });
                                }
                            }
                            break;

                        case "tagPush":
                            sql = @"select * from waiting_delivery_push where type='AppMessage' and primary_id = @primary_id";
                            var checkTagPush = await conn.QueryAsync(sql, new { primary_id = json.MessageID });

                            if (checkTagPush.Any())
                            {
                                sql = @"update waiting_delivery_push set tag_json = @tag_json where type='AppMessage' and primary_id = @primary_id";
                                await conn.QueryAsync(sql, new { tag_json = "[" + dataInfo.PushData + "]", primary_id = json.MessageID });
                            }
                            else
                            {
                                sql = @"INSERT INTO waiting_delivery_push(delivery_status,type,primary_id,start_push_time,member_json,
                                        tag_json,delivery_count,start_schedule_time,start_delivery_time,complete_time,created,created_aid)
                                        VALUES(@delivery_status,@type,@primary_id,@start_push_time,@member_json,@tag_json,@delivery_count,
                                        @start_schedule_time,@start_delivery_time,@complete_time,@created,@created_aid)";
                                await conn.QueryAsync(sql, new
                                {
                                    delivery_status = 1,
                                    type = "AppMessage",
                                    primary_id = json.MessageID,
                                    start_push_time = dataInfo.PushTime.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                    member_json = "",
                                    tag_json = "[" + dataInfo.PushData + "]",
                                    delivery_count = 0,
                                    start_schedule_time = dataInfo.PushTime.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                    start_delivery_time = 0,
                                    complete_time = 0,
                                    created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                    created_aid = HttpContext.Request.Cookies["AccountAid"],
                                });
                            }
                            break;

                        case "memberPush":
                            sql = @"select * from waiting_delivery_push where type='AppMessage' and primary_id = @primary_id";
                            var checkMemberPush = await conn.QueryAsync(sql, new { primary_id = json.MessageID });

                            if (checkMemberPush.Any())
                            {
                                sql = @"update waiting_delivery_push set member_json = @member_json where type='AppMessage' and primary_id = @primary_id";
                                await conn.QueryAsync(sql, new { member_json = "[" + dataInfo.PushData + "]", primary_id = json.MessageID });
                            }
                            else
                            {
                                sql = @"INSERT INTO waiting_delivery_push(delivery_status,type,primary_id,start_push_time,member_json,
                                        tag_json,delivery_count,start_schedule_time,start_delivery_time,complete_time,created,created_aid)
                                        VALUES(@delivery_status,@type,@primary_id,@start_push_time,@member_json,@tag_json,@delivery_count,
                                        @start_schedule_time,@start_delivery_time,@complete_time,@created,@created_aid)";
                                await conn.QueryAsync(sql, new
                                {
                                    delivery_status = 1,
                                    type = "AppMessage",
                                    primary_id = json.MessageID,
                                    start_push_time = dataInfo.PushTime.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                    member_json = "[" + dataInfo.PushData + "]",
                                    tag_json = "",
                                    delivery_count = 0,
                                    start_schedule_time = dataInfo.PushTime.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                    start_delivery_time = 0,
                                    complete_time = 0,
                                    created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                    created_aid = HttpContext.Request.Cookies["AccountAid"],
                                });
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

        public async Task<int> InsertVersion(string data)
        {
            try
            {
                using (var conn = _context.CreateConnection())
                {
                    var originalData = JsonConvert.DeserializeObject<MessageData>(data);
                    string sql = @"INSERT INTO app_message_version(app_message_id,version,status,deleted,type,
                            brand,show_time,title,content,icon_fid,image_fid,cover_image_fid,video_fid,click_title,
                            click_type,click_value,start_time,keep_time,end_time,ver_created_origin,ver_created,ver_created_aid)
                            VALUES(@app_message_id,@version,@status,@deleted,@type,@brand,@show_time,@title,
                            @content,@icon_fid,@image_fid,@cover_image_fid,@video_fid,@click_title,@click_type,@click_value,
                            @start_time,@keep_time,@end_time,@ver_created_origin,@ver_created,@ver_created_aid);";
                    await conn.QueryAsync(sql, new
                    {
                        app_message_id = originalData.App_message_id,
                        version = originalData.Version,
                        status = originalData.Status,
                        deleted = 0,
                        type = originalData.Type,
                        brand = originalData.Brand,
                        show_time = originalData.Show_time,
                        title = originalData.Title,
                        content = originalData.Content,
                        icon_fid = originalData.Icon_fid,
                        image_fid = originalData.Image_fid,
                        cover_image_fid = originalData.Cover_image_fid,
                        video_fid = originalData.Video_fid,
                        click_title = originalData.Click_title,
                        click_type = originalData.Click_type,
                        click_value = originalData.Click_value,
                        start_time = originalData.Start_time,
                        keep_time = originalData.Keep_time,
                        end_time = originalData.End_time,
                        ver_created_origin = "Web",
                        ver_created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                        ver_created_aid = HttpContext.Request.Cookies["AccountAid"],
                    });

                    return (int)originalData.Version;
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
public class MessageData
{
    public int? App_message_id { get; set; }
    public int? Version { get; set; }
    public int? Status { get; set; }
    public string? Brand { get; set; }
    public string? Type { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? App_image { get; set; }
    public string? Activity_image { get; set; }
    public string? Cover_image { get; set; }
    public string? Video { get; set; }
    public string? Click_title { get; set; }
    public string? Click_type { get; set; }
    public string? Click_value { get; set; }
    public int? Keep_time { get; set; }
    public string? CreateDate { get; set; }
    public string? TagsData { get; set; }
    public int? Show_time { get; set; }
    public int? Icon_fid { get; set; }
    public int? Image_fid { get; set; }
    public int? Cover_image_fid { get; set; }
    public int? Video_fid { get; set; }
    public int? Start_time { get; set; }
    public int? End_time { get; set; }
    public string? Changed_origin { get; set; }
    public string? Created_origin { get; set; }
    public DateTimeOffset PushTime { get; set; }
    public string? PushData { get; set; }
}
