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
    public class NewsController : ControllerBase
    {
        private readonly ILogger<NewsController> _logger;
        private readonly DapperContext _context;
        private readonly IFilesService _filesService;

        public NewsController(ILogger<NewsController> logger, DapperContext context, IFilesService filesService)
        {
            _logger = logger;
            _context = context;
            _filesService = filesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetNews(string type, int id, int version)
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
                            sql = @"SELECT * FROM app_news WHERE deleted = 0 order by created desc";

                            returnData = await conn.QueryAsync(sql);
                            break;
                        case "edit":
                            sql = @"SELECT * FROM app_news WHERE app_news_id = @app_news_id";
                            returnData = await conn.QueryAsync(sql, new
                            {
                                app_news_id = id,
                            });
                            break;
                        case "editVersion":
                            sql = @"SELECT * FROM app_news_version WHERE app_news_id = @app_news_id and version = @version";
                            returnData = await conn.QueryAsync(sql, new
                            {
                                app_news_id = id,
                                version = version,
                            });
                            break;
                        case "tags":
                            sql = @"SELECT * FROM app_news_tags WHERE app_news_id = @app_news_id";
                            returnData = await conn.QueryAsync(sql, new
                            {
                                app_news_id = id,
                            });
                            break;
                        case "version":
                            sql = @"SELECT * from app_news_version WHERE app_news_id = @app_news_id";
                            returnData = await conn.QueryAsync(sql, new
                            {
                                app_news_id = id,
                            });
                            break;

                        case "export":
                            sql = @"SELECT * from app_news WHERE app_news_id = @app_news_id";

                            returnData = await conn.QueryAsync(sql, new
                            {
                                app_news_id = id,
                            });

                            return DownloadTextFile(returnData.ToList());
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
        public async Task<IActionResult> NewNews(FrontEndData json)
        {
            try
            {
                string sql = "";
                string type = json.Type;
                var dataInfo = JsonConvert.DeserializeObject<NewsData>(json.DataInfo);
                dynamic returnData = "Success";
                int image_fid = 0;
                int icon_fid = 0;

                using (var conn = _context.CreateConnection())
                {
                    switch (type)
                    {
                        case "new":
                            if (dataInfo.Activity_image != null) { image_fid = await _filesService.UploadFilesToGCS(dataInfo, "image", "app_news"); }
                            if (dataInfo.App_image != null) { icon_fid = await _filesService.UploadFilesToGCS(dataInfo, "icon", "app_news"); }

                            sql = @"insert into app_news (version,status,deleted,archive_time,type,brand,show_time,title,content,icon_fid,
                                    image_fid,video_fid,click_title,click_type,click_value,start_time,end_time,changed,changed_aid,created,
                                    created_aid,push_source) values(@version,@status,@deleted,@archive_time,@type,@brand,@show_time,@title,
                                    @content,@icon_fid,@image_fid,@video_fid,@click_title,@click_type,@click_value,@start_time,@end_time,
                                    @changed,@changed_aid,@created,@created_aid,@push_source);

                                    SELECT CAST(LAST_INSERT_ID() AS UNSIGNED INTEGER)";

                            var app_news_id = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                            {
                                version = 1,
                                status = dataInfo.Status,
                                deleted = 0,
                                archive_time = 0,
                                type = dataInfo.Type,
                                brand = dataInfo.Brand,
                                show_time = DateTimeOffset.Parse(dataInfo.Time[0]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                title = dataInfo.Title,
                                content = dataInfo.Content,
                                icon_fid = icon_fid,
                                image_fid = image_fid,
                                video_fid = 0,
                                click_title = dataInfo.Click_title,
                                click_type = dataInfo.Click_type,
                                click_value = dataInfo.Click_value,
                                start_time = DateTimeOffset.Parse(dataInfo.Time[0]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                end_time = DateTimeOffset.Parse(dataInfo.Time[1]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                changed = 0,
                                changed_aid = 0,
                                created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                created_aid = HttpContext.Request.Cookies["AccountAid"],
                                push_source = dataInfo.Push_source
                            });

                            if (dataInfo.Push_source == "backend")
                            {
                                sql = @"INSERT INTO waiting_delivery_push(delivery_status,type,primary_id,start_push_time,member_json,
                                        tag_json,delivery_count,start_schedule_time,start_delivery_time,complete_time,created,created_aid)
                                        VALUES(@delivery_status,@type,@primary_id,@start_push_time,@member_json,@tag_json,@delivery_count,
                                        @start_schedule_time,@start_delivery_time,@complete_time,@created,@created_aid)";
                                await conn.QueryAsync(sql, new
                                {
                                    delivery_status = 1,
                                    type = "AppNews",
                                    primary_id = app_news_id,
                                    start_push_time = DateTimeOffset.Parse(dataInfo.Time[0]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                    member_json = "[]",
                                    tag_json = "[]",
                                    delivery_count = 0,
                                    start_schedule_time = DateTimeOffset.Parse(dataInfo.Time[0]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                    start_delivery_time = 0,
                                    complete_time = 0,
                                    created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                    created_aid = HttpContext.Request.Cookies["AccountAid"],
                                });
                            }
                            break;

                        case "edit":
                            int originalVersion = await InsertVersion(json.OriginalData);

                            if (dataInfo.Activity_image != null) { image_fid = await _filesService.UploadFilesToGCS(dataInfo, "image", "app_news"); }
                            if (dataInfo.App_image != null) { icon_fid = await _filesService.UploadFilesToGCS(dataInfo, "icon", "app_news"); }

                            if (image_fid == 0)
                            {
                                sql = @"select image_fid from app_news where app_news_id = @app_news_id";
                                image_fid = await conn.QuerySingleOrDefaultAsync<int>(sql, new { app_news_id = json.NewsID });
                            }

                            if (icon_fid == 0)
                            {
                                sql = @"select icon_fid from app_news where app_news_id = @app_news_id";
                                icon_fid = await conn.QuerySingleOrDefaultAsync<int>(sql, new { app_news_id = json.NewsID });
                            }

                            sql = @"update app_news set version = @version, status = @status, type = @type, brand = @brand, show_time = @show_time,
                                    title = @title, content = @content, icon_fid = @icon_fid, image_fid = @image_fid, click_title = @click_title,
                                    click_type = @click_type, click_value = @click_value, start_time = @start_time, end_time = @end_time, 
                                    changed = @changed, changed_aid = @changed_aid where app_news_id = @app_news_id";

                            await conn.QueryAsync(sql, new
                            {
                                version = originalVersion + 1,
                                status = dataInfo.Status,
                                type = dataInfo.Type,
                                brand = dataInfo.Brand,
                                show_time = DateTimeOffset.Parse(dataInfo.Time[0]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                title = dataInfo.Title,
                                content = dataInfo.Content,
                                icon_fid = icon_fid,
                                image_fid = image_fid,
                                click_title = dataInfo.Click_title,
                                click_type = dataInfo.Click_type,
                                click_value = dataInfo.Click_value,
                                start_time = DateTimeOffset.Parse(dataInfo.Time[0]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                end_time = DateTimeOffset.Parse(dataInfo.Time[1]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                changed = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                changed_aid = HttpContext.Request.Cookies["AccountAid"],
                                app_news_id = json.NewsID,
                            });
                            break;

                        case "delete":
                            sql = @"update app_news set deleted = @status where app_news_id = @app_news_id";
                            returnData = await conn.QueryAsync(sql, new
                            {
                                status = dataInfo.Status,
                                app_news_id = json.NewsID,
                            });
                            sql = @"DELETE FROM waiting_delivery_push WHERE primary_id = @primary_id";
                            await conn.QueryAsync(sql, new
                            {
                                primary_id = json.NewsID,
                            });
                            break;

                        case "tags":
                            string[] tags = new string[1];
                            sql = @"delete from app_news_tags where app_news_id = @app_news_id";
                            await conn.QueryAsync(sql, new { app_news_id = json.NewsID });

                            if (dataInfo.TagsData.Any())
                            {
                                tags = dataInfo.TagsData.Split(',');
                                sql = @"insert into app_news_tags(app_news_id, tag_id)values(@app_news_id, @tagsID)";

                                foreach (string tag in tags)
                                {
                                    returnData = conn.QueryAsync<int>(sql, new
                                    {
                                        app_news_id = json.NewsID,
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

        public async Task<int> InsertVersion(string data)
        {
            try
            {
                using (var conn = _context.CreateConnection())
                {
                    var originalData = JsonConvert.DeserializeObject<NewsData>(data);
                    string sql = @"INSERT INTO app_news_version(app_news_id,version,status,deleted,type,
                            brand,show_time,title,content,icon_fid,image_fid,video_fid,click_title,
                            click_type,click_value,start_time,end_time,ver_created,ver_created_aid)
                            VALUES(@app_news_id,@version,@status,@deleted,@type,@brand,@show_time,@title,
                            @content,@icon_fid,@image_fid,@video_fid,@click_title,@click_type,@click_value,
                            @start_time,@end_time,@ver_created,@ver_created_aid);";
                    await conn.QueryAsync(sql, new
                    {
                        app_news_id = originalData.App_news_id,
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
                        video_fid = originalData.Video_fid,
                        click_title = originalData.Click_title,
                        click_type = originalData.Click_type,
                        click_value = originalData.Click_value,
                        start_time = originalData.Start_time,
                        end_time = originalData.End_time,
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

        public FileResult DownloadTextFile(List<dynamic> data)
        {
            string type = data[0].type switch
            {
                "discount" => "優惠訊息",
                "member" => "會員訊息",
                "order" => "訂單訊息",
                _ => ""
            };
            DateTime show_time = DateTimeOffset.FromUnixTimeSeconds(data[0].show_time).DateTime;

            string content = $"origin_id:{data[0].app_news_id}\n" +
                $"content_source:{data[0].push_source}\n" +
                $"brand:{data[0].brand}\n" +
                $"type:{type}\n" +
                $"show_time:{show_time:yyyy/MM/dd} \n" +
                $"title:{data[0].title} \n" +
                $"content:{data[0].content} \n" +
                $"click_type:{data[0].click_type}\n" +
                $"click_title:{data[0].click_title}\n" +
                $"click_value:{data[0].click_value}";

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);

            string mimeType = "text/plain";

            string fileName = $"最新消息({data[0].app_news_id}).txt";

            return File(bytes, mimeType, fileName);
        }
    }
}
public class NewsData
{
    public int? App_news_id { get; set; }
    public int? Version { get; set; }
    public int? Status { get; set; }
    public string? Brand { get; set; }
    public string? Type { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string[]? Time { get; set; }
    public string? App_image { get; set; }
    public string? Activity_image { get; set; }
    public string? Video { get; set; }
    public string? Click_title { get; set; }
    public string? Click_type { get; set; }
    public string? Click_value { get; set; }
    public string? Push_source { get; set; }
    public string? CreateDate { get; set; }
    public string? TagsData { get; set; }
    public int? Show_time { get; set; }
    public int? Icon_fid { get; set; }
    public int? Image_fid { get; set; }
    public int? Video_fid { get; set; }
    public int? Start_time { get; set; }
    public int? End_time { get; set; }
}
