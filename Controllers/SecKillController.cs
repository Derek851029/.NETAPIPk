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
    public class SecKillController : ControllerBase
    {
        private readonly DapperContext _context;
        private readonly ILogger<SecKillController> _logger;
        private readonly IFilesService _filesService;

        public SecKillController(DapperContext context, ILogger<SecKillController> logger, IFilesService filesService)
        {
            _context = context;
            _logger = logger;
            _filesService = filesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetSeckill(string type, int day)
        {
            try
            {
                string sql = "";
                IEnumerable<dynamic> returnData = null;
                //今天0點時間
                DateTimeOffset zeroNow = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

                //將前端timestamp轉換成datetime
                DateTimeOffset parseFrontDay = DateTimeOffset.FromUnixTimeSeconds(day);
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
                            sql = @"select * from app_seckill where deleted = 0 order by created desc";
                            returnData = await conn.QueryAsync(sql);
                            break;
                        case "sort":
                            sql = @"select * from app_seckill_sort where show_time = @FrontTimestamp";
                            var data = await conn.QueryAsync(sql, new { FrontTimestamp = FrontTimestamp });
                            if (data.Any())
                            {
                                sql = @"select * from app_seckill_sort a left join app_seckill b on a.app_seckill_id = b.app_seckill_id
                                        where a.show_time = @FrontTimestamp order by sortweight asc";
                                var hasSortData = await conn.QueryAsync(sql, new { FrontTimestamp = FrontTimestamp });

                                sql = @"select * from app_seckill where deleted = 0 and end_time >= @FrontTimestamp and  
                                        start_time <= @FrontEndTimestamp and status = 1 order by end_time asc";
                                var newData = await conn.QueryAsync(sql, new { FrontTimestamp = FrontTimestamp, FrontEndTimestamp = FrontEndTimestamp });

                                returnData = hasSortData.Concat(newData)
                                            .GroupBy(x => x.app_seckill_id)
                                            .Select(group => group.First())
                                            .ToList();

                            }
                            else
                            {
                                sql = @"select * from app_seckill where deleted = 0 and end_time > @FrontTimestamp and  
                                        start_time <= @FrontEndTimestamp and status = 1 order by end_time asc";
                                returnData = await conn.QueryAsync(sql, new { FrontTimestamp = FrontTimestamp, FrontEndTimestamp = FrontEndTimestamp });
                            }

                            break;
                        case "tags":
                            sql = @"select * from app_seckill_tags where app_seckill_id = @app_seckill_id";
                            //這裡的day帶的是app_seckill_id
                            returnData = await conn.QueryAsync(sql, new { app_seckill_id = day });
                            break;
                    }
                    return Ok(returnData.ToList());
                }
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
                DateTimeOffset zeroNow = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

                // 獲取當天0點時間
                Int32 zeroTimestamp = (Int32)zeroNow.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                Int32 nextTimestamp = (Int32)DateTimeOffset.Now.AddDays(1).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();

                string sql = "";
                string type = json.Type;
                var dataInfo = JsonConvert.DeserializeObject<SeckillData>(json.DataInfo);
                dynamic returnData = "";

                int image_fid = 0;

                using (var conn = _context.CreateConnection())
                {
                    switch (type)
                    {
                        case "view":
                            sql = @"select * from app_seckill where app_seckill_id = @id";
                            returnData = await conn.QuerySingleOrDefaultAsync(sql, new { id = json.SeckillID });
                            break;

                        case "new":
                            if (dataInfo.Activity_image != null) { image_fid = await _filesService.UploadFilesToGCS(dataInfo, "image", "app_seckill"); }

                            sql = @"insert into app_seckill(brand, act_title, app_title, app_sub_title,start_time, end_time, 
                                product_url, image_fid, created, created_aid, 
                                changed, changed_aid, status, deleted) values(@brand, @act_title, @app_title,@app_sub_title, @start_time, 
                                @end_time, @product_url, @image_fid, @created, @created_aid, @changed, 
                                @changed_aid, @status, @deleted);

                                SELECT CAST(LAST_INSERT_ID() AS UNSIGNED INTEGER)";
                            returnData = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                            {
                                brand = dataInfo.Brand,
                                act_title = dataInfo.Act_title,
                                app_title = dataInfo.App_title,
                                app_sub_title = dataInfo.App_sub_title ?? "",
                                start_time = DateTimeOffset.Parse(dataInfo.Time[0]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                end_time = DateTimeOffset.Parse(dataInfo.Time[1]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                product_url = dataInfo.Product_url,
                                image_fid = image_fid,
                                created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                created_aid = HttpContext.Request.Cookies["AccountAid"],
                                changed = 0,
                                changed_aid = 0,
                                status = 1,
                                deleted = 0,
                            });
                            break;

                        case "edit":
                            if (dataInfo.Activity_image != null) { image_fid = await _filesService.UploadFilesToGCS(dataInfo, "image", "app_seckill"); }

                            if (image_fid == 0)
                            {
                                sql = @"select image_fid from app_seckill where app_seckill_id = @seckillID";
                                image_fid = await conn.QuerySingleOrDefaultAsync<int>(sql, new { seckillID = json.SeckillID });
                            }

                            sql = @"update app_seckill set brand = @brand, act_title = @act_title, app_title = @app_title, app_sub_title = @app_sub_title,
                                start_time = @start_time, end_time = @end_time, product_url = @product_url,
                                image_fid = @image_fid, changed = @changed, changed_aid = @changed_aid where app_seckill_id = @seckillID";

                            returnData = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                            {
                                seckillID = json.SeckillID,
                                brand = dataInfo.Brand,
                                act_title = dataInfo.Act_title,
                                app_title = dataInfo.App_title,
                                app_sub_title = dataInfo.App_sub_title ?? "",
                                start_time = DateTimeOffset.Parse(dataInfo.Time[0]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                end_time = DateTimeOffset.Parse(dataInfo.Time[1]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                product_url = dataInfo.Product_url,
                                image_fid = image_fid,
                                changed = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                changed_aid = HttpContext.Request.Cookies["AccountAid"],
                            });
                            break;

                        case "status":
                            sql = @"update app_seckill set status = @status where app_seckill_id = @seckillID";
                            returnData = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                            {
                                status = dataInfo.Status,
                                seckillID = json.SeckillID,
                            });
                            break;

                        case "delete":
                            sql = @"update app_seckill set deleted = @status where app_seckill_id = @seckillID";
                            returnData = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                            {
                                status = dataInfo.Status,
                                seckillID = json.SeckillID,
                            });
                            break;

                        case "sort":
                            DateTimeOffset parseFrontDay = DateTimeOffset.FromUnixTimeSeconds((long)json.SortDate);
                            DateTimeOffset zeroFrontDay = new DateTime(parseFrontDay.Year, parseFrontDay.Month, parseFrontDay.Day);
                            //前端0點timestamp
                            Int32 FrontTimestamp = (Int32)zeroFrontDay.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                            sql = @"select * from app_seckill_sort where show_time = @show_time";
                            var data = await conn.QueryAsync(sql, new { show_time = FrontTimestamp });

                            if (data.Any())
                            {
                                sql = @"DELETE FROM app_seckill_sort WHERE show_time = @show_time";
                                await conn.QueryAsync(sql, new { show_time = FrontTimestamp });
                            }

                            sql = @"insert into app_seckill_sort(app_seckill_id, show_time, sortweight) 
                                values(@app_seckill_id, @show_time, @sortweight)";
                            JArray jArray = JArray.Parse(dataInfo.SortData);
                            for (int i = 0; i < jArray.Count; i++)
                            {
                                JObject jObject = JObject.Parse(jArray[i].ToString());
                                returnData = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                                {
                                    app_seckill_id = (int)jObject["app_seckill_id"],
                                    show_time = FrontTimestamp,
                                    sortweight = i + 1,
                                });
                            }
                            break;

                        case "tags":
                            string[] tags = new string[1];
                            sql = @"delete from app_seckill_tags where app_seckill_id = @seckillID";
                            await conn.QueryAsync(sql, new { seckillID = json.SeckillID });

                            if (dataInfo.TagsData.Any())
                            {
                                tags = dataInfo.TagsData.Split(',');
                                sql = @"insert into app_seckill_tags(app_seckill_id, tag_id)values(@seckillID, @tagsID)";

                                foreach (string tag in tags)
                                {
                                    returnData = conn.QueryAsync<int>(sql, new
                                    {
                                        seckillID = json.SeckillID,
                                        tagsID = tag,
                                    });
                                }
                            }

                            break;
                    }
                    return Ok(returnData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }

        }


    }
}

public class SeckillData
{
    public string? SortData { get; set; }
    public string? TagsData { get; set; }
    public string? Act_title { get; set; }
    public string? Brand { get; set; }
    public string? App_title { get; set; }
    public string? App_sub_title { get; set; }
    public string[]? Time { get; set; }
    public string? Product_url { get; set; }
    public string? Activity_image { get; set; }
    public string? CreateDate { get; set; }
    public int? Status { get; set; }
    public int? app_seckill_id { get; set; }
}

public class ImageData
{
    public string? uid { get; set; }
    public string? lastModified { get; set; }
    public string? lastModifiedDate { get; set; }
    public string? name { get; set; }
    public string? size { get; set; }
    public string? type { get; set; }
    public string? percent { get; set; }
    public string? originFileObj { get; set; }
    public string? status { get; set; }
    public string? response { get; set; }
    public string? xhr { get; set; }
    public string? thumbUrl { get; set; }
}


