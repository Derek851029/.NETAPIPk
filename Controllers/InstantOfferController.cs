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
    public class InstantOfferController : ControllerBase
    {
        private readonly ILogger<InstantOfferController> _logger;
        private readonly DapperContext _context;
        private readonly IFilesService _filesService;

        public InstantOfferController(ILogger<InstantOfferController> logger, DapperContext context, IFilesService filesService)
        {
            _logger = logger;
            _context = context;
            _filesService = filesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetInstantOffer(int id, string type)
        {
            try
            {
                string sql = "";
                IEnumerable<dynamic> returnData = null;
                //今天0點時間
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
                            sql = @"select * from app_instant_offer where deleted = 0 order by created desc";
                            returnData = await conn.QueryAsync(sql);
                            break;
                        case "edit":
                            sql = @"select * from app_instant_offer where deleted = 0 and app_instant_offer_id = @id";
                            returnData = await conn.QueryAsync(sql, new { id = id });
                            break;
                        case "tags":
                            sql = @"select * from app_instant_offer_tags where app_instant_offer_id = @app_instant_offer_id";
                            //這裡的day帶的是app_seckill_id
                            returnData = await conn.QueryAsync(sql, new { app_instant_offer_id = id });
                            break;
                        case "sort":
                            sql = @"select * from app_instant_offer_sort where show_time = @FrontTimestamp";
                            var data = await conn.QueryAsync(sql, new { FrontTimestamp = FrontTimestamp });
                            if (data.Any())
                            {
                                sql = @"select * from app_instant_offer_sort a left join app_instant_offer b on a.app_instant_offer_id = b.app_instant_offer_id
                                        where a.show_time = @FrontTimestamp order by sortweight asc";
                                var hasSortData = await conn.QueryAsync(sql, new { FrontTimestamp = FrontTimestamp });

                                sql = @"select * from app_instant_offer where deleted = 0 and end_time >= @FrontTimestamp and  
                                        start_time <= @FrontEndTimestamp and status = 1 order by end_time asc";
                                var newData = await conn.QueryAsync(sql, new { FrontTimestamp = FrontTimestamp, FrontEndTimestamp = FrontEndTimestamp });

                                returnData = hasSortData.Concat(newData)
                                            .GroupBy(x => x.app_instant_offer_id)
                                            .Select(group => group.First())
                                            .ToList();

                            }
                            else
                            {
                                sql = @"select * from app_instant_offer where deleted = 0 and end_time > @FrontTimestamp and  
                                        start_time <= @FrontEndTimestamp and status = 1 order by end_time asc";
                                returnData = await conn.QueryAsync(sql, new { FrontTimestamp = FrontTimestamp, FrontEndTimestamp = FrontEndTimestamp });
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

        [HttpPost]
        public async Task<IActionResult> InstantOfferData(FrontEndData json)
        {
            try
            {
                DateTimeOffset zeroNow = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

                // 獲取當天0點時間
                Int32 zeroTimestamp = (Int32)zeroNow.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                Int32 nextTimestamp = (Int32)DateTimeOffset.Now.AddDays(1).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                string sql = "";

                var dataInfo = JsonConvert.DeserializeObject<InstantOfferData>(json.DataInfo);

                int image_fid = 0;
                dynamic returnData = null;

                using (var conn = _context.CreateConnection())
                {
                    switch (json.Type)
                    {
                        case "new":
                            if (dataInfo.Activity_image != null) { image_fid = await _filesService.UploadFilesToGCS(dataInfo, "image", "instant_offer"); }

                            sql = @"insert into app_instant_offer(brand, app_instant_offer_name,app_instant_offer_title,app_instant_offer_sub_title,
                                    start_time,end_time,time_frequency,min_sales,product_url,image_fid,created,created_aid,
                                    changed,changed_aid,status,deleted) 
                                    values(@brand, @app_instant_offer_name,@app_instant_offer_title,@app_instant_offer_sub_title,@start_time,@end_time,
                                    @time_frequency, @min_sales, @product_url,@image_fid,@created,@created_aid,
                                    @changed,@changed_aid,@status,@deleted)";
                            returnData = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                            {
                                brand = dataInfo.Brand,
                                app_instant_offer_name = dataInfo.App_instant_offer_name,
                                app_instant_offer_title = dataInfo.App_instant_offer_title,
                                app_instant_offer_sub_title = dataInfo.App_instant_offer_sub_title ?? "",
                                start_time = DateTimeOffset.Parse(dataInfo.Time[0]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                end_time = DateTimeOffset.Parse(dataInfo.Time[1]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                time_frequency = dataInfo.Time_frequency,
                                min_sales = dataInfo.Min_sales,
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
                            if (dataInfo.Activity_image != null) { image_fid = await _filesService.UploadFilesToGCS(dataInfo, "image", "instant_offer"); }

                            if (image_fid == 0)
                            {
                                sql = @"select image_fid from app_instant_offer where app_instant_offer_id = @app_instant_offer_id";
                                image_fid = await conn.QuerySingleOrDefaultAsync<int>(sql, new { app_instant_offer_id = json.App_instant_offer_id });
                            }

                            sql = @"update app_instant_offer set brand = @brand, app_instant_offer_name = @app_instant_offer_name, app_instant_offer_title = @app_instant_offer_title, 
                                    app_instant_offer_sub_title = @app_instant_offer_sub_title, start_time = @start_time, end_time = @end_time, 
                                    time_frequency = @time_frequency, min_sales = @min_sales, product_url = @product_url,
                                    image_fid = @image_fid, changed = @changed, changed_aid = @changed_aid 
                                    where app_instant_offer_id = @app_instant_offer_id";
                            returnData = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                            {
                                brand = dataInfo.Brand,
                                app_instant_offer_name = dataInfo.App_instant_offer_name,
                                app_instant_offer_title = dataInfo.App_instant_offer_title,
                                app_instant_offer_sub_title = dataInfo.App_instant_offer_sub_title ?? "",
                                start_time = DateTimeOffset.Parse(dataInfo.Time[0]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                end_time = DateTimeOffset.Parse(dataInfo.Time[1]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                time_frequency = dataInfo.Time_frequency,
                                min_sales = dataInfo.Min_sales,
                                product_url = dataInfo.Product_url,
                                image_fid = image_fid,
                                changed = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                changed_aid = HttpContext.Request.Cookies["AccountAid"],
                                app_instant_offer_id = json.App_instant_offer_id,
                            });
                            break;
                        case "status":
                            sql = @"update app_instant_offer set status = @status where app_instant_offer_id = @app_instant_offer_id";
                            returnData = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                            {
                                status = dataInfo.Status,
                                app_instant_offer_id = json.App_instant_offer_id,
                            });
                            break;
                        case "delete":
                            sql = @"update app_instant_offer set deleted = @status where app_instant_offer_id = @app_instant_offer_id";
                            returnData = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                            {
                                status = dataInfo.Status,
                                app_instant_offer_id = json.App_instant_offer_id,
                            });
                            break;
                        case "sort":
                            DateTimeOffset parseFrontDay = DateTimeOffset.FromUnixTimeSeconds((long)json.SortDate);
                            DateTimeOffset zeroFrontDay = new DateTime(parseFrontDay.Year, parseFrontDay.Month, parseFrontDay.Day);
                            //前端0點timestamp
                            Int32 FrontTimestamp = (Int32)zeroFrontDay.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                            sql = @"select * from app_instant_offer_sort where show_time = @show_time";
                            var data = await conn.QueryAsync(sql, new { show_time = FrontTimestamp });

                            if (data.Any())
                            {
                                sql = @"DELETE FROM app_instant_offer_sort WHERE show_time = @show_time";
                                await conn.QueryAsync(sql, new { show_time = FrontTimestamp });
                            }

                            sql = @"insert into app_instant_offer_sort(app_instant_offer_id, show_time, sortweight) 
                                values(@app_instant_offer_id, @show_time, @sortweight)";
                            JArray jArray = JArray.Parse(dataInfo.SortData);
                            for (int i = 0; i < jArray.Count; i++)
                            {
                                JObject jObject = JObject.Parse(jArray[i].ToString());
                                returnData = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                                {
                                    app_instant_offer_id = (int)jObject["app_instant_offer_id"],
                                    show_time = FrontTimestamp,
                                    sortweight = i + 1,
                                });
                            }
                            break;
                        case "tags":
                            string[] tags = new string[1];

                            sql = @"delete from app_instant_offer_tags where app_instant_offer_id = @app_instant_offer_id";
                            await conn.QueryAsync(sql, new { app_instant_offer_id = json.App_instant_offer_id });

                            if (dataInfo.TagsData.Any())
                            {
                                tags = dataInfo.TagsData.Split(',');
                                sql = @"insert into app_instant_offer_tags(app_instant_offer_id, tag_id)values(@app_instant_offer_id, @tagsID)";

                                foreach (string tag in tags)
                                {
                                    returnData = conn.QueryAsync<int>(sql, new
                                    {
                                        app_instant_offer_id = json.App_instant_offer_id,
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

public class InstantOfferData
{
    public string? App_instant_offer_name { get; set; }
    public string? SortData { get; set; }
    public string? TagsData { get; set; }
    public string? Brand { get; set; }
    public string? App_instant_offer_title { get; set; }
    public string? App_instant_offer_sub_title { get; set; }
    public string[]? Time { get; set; }
    public int? Time_frequency { get; set; }
    public int? Min_sales { get; set; }
    public string? Product_url { get; set; }
    public string? Activity_image { get; set; }
    public string? CreateDate { get; set; }
    public int? Status { get; set; }
}
