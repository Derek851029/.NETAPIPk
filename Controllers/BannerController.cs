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
    public class BannerController : ControllerBase
    {
        private readonly ILogger<BannerController> _logger;
        private readonly DapperContext _context;
        private readonly IFilesService _filesService;

        public BannerController(ILogger<BannerController> logger, DapperContext context, IFilesService filesService)
        {
            _logger = logger;
            _context = context;
            _filesService = filesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetBanner(string type, int id) //這裡的ID是banner_ID 也是sort的day
        {
            try
            {
                string sql = "";
                IEnumerable<dynamic> returnData = null;

                DateTimeOffset zeroNow = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

                //將前端timestamp轉換成datetime
                DateTimeOffset parseFrontDay = DateTimeOffset.FromUnixTimeSeconds(id);
                //前端當天0時間
                DateTimeOffset zeroFrontDay = new DateTime(parseFrontDay.Year, parseFrontDay.Month, parseFrontDay.Day);
                //前端當天59時間
                DateTimeOffset endFrontDay = new DateTime(parseFrontDay.Year, parseFrontDay.Month, parseFrontDay.Day, 23, 59, 59);

                //前端0點timestamp
                Int32 FrontTimestamp = (Int32)zeroFrontDay.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                //前端當天59timestamp
                Int32 FrontEndTimestamp = (Int32)endFrontDay.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();

                //今天0點timestamp
                Int32 zeroTimestamp = (Int32)zeroNow.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                Int32 nextTimestamp = (Int32)DateTimeOffset.Now.AddDays(1).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                using (var conn = _context.CreateConnection())
                {
                    switch (type)
                    {
                        case "list":
                            sql = @"select a.*,b.name from app_banner a
                                left join administrators_profile b on 
                                CASE WHEN a.changed_aid <> 0 THEN a.changed_aid ELSE a.created_aid END = b.aid
                                where a.deleted = 0
                                order by a.created desc";

                            returnData = await conn.QueryAsync(sql);
                            break;
                        case "edit":
                            sql = @"select a.*,b.name from app_banner a
                                left join administrators_profile b on 
                                CASE WHEN a.changed_aid <> 0 THEN a.changed_aid ELSE a.created_aid END = b.aid
                                where a.deleted = 0 and a.app_banner_id = @app_banner_id
                                order by a.created desc";

                            returnData = await conn.QueryAsync(sql, new { app_banner_id = id });
                            break;
                        case "sort":
                            sql = @"select * from app_banner_sort where show_time = @FrontTimestamp";
                            var data = await conn.QueryAsync(sql, new { FrontTimestamp = FrontTimestamp });
                            if (data.Any())
                            {
                                sql = @"select * from app_banner_sort a left join app_banner b on a.app_banner_id = b.app_banner_id
                                        where a.show_time = @FrontTimestamp order by a.sortweight asc";
                                var hasSortData = await conn.QueryAsync(sql, new { FrontTimestamp = FrontTimestamp });

                                sql = @"select * from app_banner where deleted = 0 and end_time >= @FrontTimestamp 
                                        and  start_time <= @FrontEndTimestamp and status = 1 order by end_time asc";
                                var newData = await conn.QueryAsync(sql, new { FrontTimestamp = FrontTimestamp, FrontEndTimestamp = FrontEndTimestamp });

                                returnData = hasSortData.Concat(newData)
                                            .GroupBy(x => x.app_banner_id)
                                            .Select(group => group.First())
                                            .ToList();
                            }
                            else
                            {
                                sql = @"select * from app_banner where deleted = 0 and end_time >= @FrontTimestamp 
                                        and  start_time <= @FrontEndTimestamp and status = 1 order by end_time asc";
                                returnData = await conn.QueryAsync(sql, new { FrontTimestamp = FrontTimestamp, FrontEndTimestamp = FrontEndTimestamp });
                            }

                            break;
                        case "tags":
                            sql = @"select * from app_banner_tags where app_banner_id = @app_banner_id";

                            returnData = await conn.QueryAsync(sql, new { app_banner_id = id });
                            break;
                    }
                }
                return Ok(returnData.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> NewBanner(FrontEndData json)
        {
            try
            {
                DateTimeOffset zeroNow = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

                // 獲取當天0點時間
                Int32 zeroTimestamp = (Int32)zeroNow.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                Int32 nextTimestamp = (Int32)DateTimeOffset.Now.AddDays(1).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();

                string sql = "";
                string type = json.Type;
                var dataInfo = JsonConvert.DeserializeObject<BannerData>(json.DataInfo);

                dynamic returnData = "Success";

                int image_fid = 0;

                using (var conn = _context.CreateConnection())
                {
                    switch (type)
                    {
                        case "new":
                            if (dataInfo.Activity_image != null) { image_fid = await _filesService.UploadFilesToGCS(dataInfo, "image", "app_banner"); }

                            sql = @"insert into app_banner (version,status,deleted,brand,title,image_fid,video_fid,click_type,click_value,
                                    start_time,end_time,sortweight,changed,changed_aid,created,created_aid) VALUES(@version,@status,
                                    @deleted,@brand,@title,@image_fid,@video_fid,@click_type,@click_value,@start_time,@end_time,
                                    @sortweight,@changed,@changed_aid,@created,@created_aid)";

                            await conn.QueryAsync<int>(sql, new
                            {
                                version = 1,
                                status = dataInfo.Status,
                                deleted = 0,
                                brand = dataInfo.Brand,
                                title = dataInfo.Title,
                                image_fid = image_fid,
                                video_fid = 0,
                                click_type = dataInfo.Click_type,
                                click_value = dataInfo.Click_value,
                                start_time = DateTimeOffset.Parse(dataInfo.Time[0]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                end_time = DateTimeOffset.Parse(dataInfo.Time[1]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                sortweight = 50,
                                changed = 0,
                                changed_aid = 0,
                                created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                created_aid = HttpContext.Request.Cookies["AccountAid"],
                            });
                            break;

                        case "edit":
                            if (dataInfo.Activity_image != null) { image_fid = await _filesService.UploadFilesToGCS(dataInfo, "image", "app_banner"); }

                            if (image_fid == 0)
                            {
                                sql = @"select image_fid from app_banner where app_banner_id = @BannerID";
                                image_fid = await conn.QuerySingleOrDefaultAsync<int>(sql, new { BannerID = json.BannerID });
                            }

                            sql = @"update app_banner set status = @status, brand = @brand, title = @title, image_fid = @image_fid, click_type = @click_type,
                                    click_value = @click_value, start_time = @start_time, end_time = @end_time, changed = @changed, changed_aid = @changed_aid
                                    where app_banner_id = @app_banner_id;
                                    SELECT CAST(LAST_INSERT_ID() AS UNSIGNED INTEGER)";

                            returnData = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                            {
                                app_banner_id = json.BannerID,
                                status = dataInfo.Status,
                                brand = dataInfo.Brand,
                                title = dataInfo.Title,
                                image_fid = image_fid,
                                click_type = dataInfo.Click_type,
                                click_value = dataInfo.Click_value,
                                start_time = DateTimeOffset.Parse(dataInfo.Time[0]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                end_time = DateTimeOffset.Parse(dataInfo.Time[1]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                changed = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                changed_aid = HttpContext.Request.Cookies["AccountAid"],
                            });
                            break;
                        case "delete":
                            sql = @"update app_banner set deleted = @status where app_banner_id = @app_banner_id";
                            returnData = await conn.QueryAsync(sql, new
                            {
                                status = dataInfo.Status,
                                app_banner_id = json.BannerID,
                            });
                            break;

                        case "sort":
                            DateTimeOffset parseFrontDay = DateTimeOffset.FromUnixTimeSeconds((long)json.SortDate);
                            DateTimeOffset zeroFrontDay = new DateTime(parseFrontDay.Year, parseFrontDay.Month, parseFrontDay.Day);
                            //前端0點timestamp
                            Int32 FrontTimestamp = (Int32)zeroFrontDay.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();

                            sql = @"select * from app_banner_sort where show_time = @show_time";
                            var data = await conn.QueryAsync(sql, new { show_time = FrontTimestamp });

                            if (data.Any())
                            {
                                sql = @"DELETE FROM app_banner_sort WHERE show_time = @show_time";
                                await conn.QueryAsync(sql, new { show_time = FrontTimestamp });
                            }

                            sql = @"insert into app_banner_sort(app_banner_id, show_time, sortweight) 
                                values(@app_banner_id, @show_time, @sortweight)";

                            JArray jArray = JArray.Parse(dataInfo.SortData);
                            for (int i = 0; i < jArray.Count; i++)
                            {
                                JObject jObject = JObject.Parse(jArray[i].ToString());
                                returnData = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                                {
                                    app_banner_id = (int)jObject["app_banner_id"],
                                    show_time = FrontTimestamp,
                                    sortweight = i + 1,
                                });
                            }
                            break;

                        case "tags":
                            string[] tags = new string[1];
                            sql = @"delete from app_banner_tags where app_banner_id = @BannerID";
                            await conn.QueryAsync(sql, new { BannerID = json.BannerID });

                            if (dataInfo.TagsData.Any())
                            {
                                tags = dataInfo.TagsData.Split(',');
                                sql = @"insert into app_banner_tags(app_banner_id, tag_id)values(@BannerID, @tagsID)";

                                foreach (string tag in tags)
                                {
                                    returnData = conn.QueryAsync<int>(sql, new
                                    {
                                        BannerID = json.BannerID,
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
    }
}

public class BannerData
{
    public int? Status { get; set; }
    public string? Brand { get; set; }
    public string? Title { get; set; }
    public string[]? Time { get; set; }
    public string? Activity_image { get; set; }
    public string? Video { get; set; }
    public string? Click_type { get; set; }
    public string? Click_value { get; set; }
    public string? CreateDate { get; set; }
    public string? TagsData { get; set; }
    public string? SortData { get; set; }
}
