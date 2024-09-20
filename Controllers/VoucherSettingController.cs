using Dapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PKApp.ConfigOptions;
using PKApp.DIObject;
using PKApp.Services;
using System.Text;

namespace PKApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VoucherSettingController : ControllerBase
    {
        private readonly ILogger<VoucherSettingController> _logger;
        private readonly DapperContext _context;
        private readonly IFilesService _filesService;
        private readonly IVoucherSettingService _voucherSettingService;

        public VoucherSettingController(ILogger<VoucherSettingController> logger, DapperContext context,
            IFilesService filesService, IVoucherSettingService voucherSettingService)
        {
            _logger = logger;
            _context = context;
            _filesService = filesService;
            _voucherSettingService = voucherSettingService;
        }

        [HttpGet]
        public async Task<IActionResult> GetVoucher(string type, int id, int day)
        {
            try
            {
                string sql = "";
                dynamic returnData = null;

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
                            sql = @"SELECT * FROM app_voucher WHERE deleted = 0 ORDER BY created DESC";
                            returnData = await conn.QueryAsync(sql);
                            break;
                        case "edit":
                            sql = @"SELECT * FROM app_voucher WHERE deleted = 0 AND app_voucher_id = @app_voucher_id";
                            returnData = await conn.QueryAsync(sql, new { app_voucher_id = id });
                            break;
                        case "sort":
                            sql = @"select * from app_voucher_sort where show_time = @FrontTimestamp";
                            var data = await conn.QueryAsync(sql, new { FrontTimestamp = FrontTimestamp });
                            if (data.Any())
                            {
                                sql = @"select * from app_voucher_sort a left join app_voucher b on a.app_voucher_id = b.app_voucher_id
                                        where a.show_time = @FrontTimestamp order by sortweight asc";
                                var hasSortData = await conn.QueryAsync(sql, new { FrontTimestamp = FrontTimestamp });

                                sql = @"select * from app_voucher where deleted = 0 and end_time >= @FrontTimestamp and  
                                        start_time <= @FrontEndTimestamp and status = 1 order by end_time asc";
                                var newData = await conn.QueryAsync(sql, new { FrontTimestamp = FrontTimestamp, FrontEndTimestamp = FrontEndTimestamp });

                                returnData = hasSortData.Concat(newData)
                                            .GroupBy(x => x.app_voucher_id)
                                            .Select(group => group.First())
                                            .ToList();

                            }
                            else
                            {
                                sql = @"select * from app_voucher where deleted = 0 and end_time > @FrontTimestamp and  
                                        start_time <= @FrontEndTimestamp and status = 1 order by end_time asc";
                                returnData = await conn.QueryAsync(sql, new { FrontTimestamp = FrontTimestamp, FrontEndTimestamp = FrontEndTimestamp });
                            }

                            break;


                        case "strollet":

                            returnData = await GetVoucherList();
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
                DateTimeOffset zeroNow = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

                // 獲取當天0點時間
                Int32 zeroTimestamp = (Int32)zeroNow.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                Int32 nextTimestamp = (Int32)DateTimeOffset.Now.AddDays(1).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();

                string sql = "";
                string type = json.Type;
                var dataInfo = JsonConvert.DeserializeObject<VoucherData>(json.DataInfo);
                dynamic returnData = "Success";
                var image_fid = 0;

                string aid = HttpContext.Request?.Cookies["AccountAid"] ?? "0";
                int? id = json.VoucherId;
                int voucherId = (int)(id == null ? 0 : json.VoucherId);
                using (var conn = _context.CreateConnection())
                {
                    switch (type)
                    {
                        case "new":
                            voucherId = await _voucherSettingService.HandleDBAsync(dataInfo, 0, aid);
                            break;

                        case "edit":
                            voucherId = await _voucherSettingService.HandleDBAsync(dataInfo, voucherId, aid);
                            break;

                        case "sort":
                            DateTimeOffset parseFrontDay = DateTimeOffset.FromUnixTimeSeconds((long)json.SortDate);
                            DateTimeOffset zeroFrontDay = new DateTime(parseFrontDay.Year, parseFrontDay.Month, parseFrontDay.Day);
                            //前端0點timestamp
                            Int32 FrontTimestamp = (Int32)zeroFrontDay.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                            sql = @"select * from app_voucher_sort where show_time = @show_time";
                            var data = await conn.QueryAsync(sql, new { show_time = FrontTimestamp });

                            if (data.Any())
                            {
                                sql = @"DELETE FROM app_voucher_sort WHERE show_time = @show_time";
                                await conn.QueryAsync(sql, new { show_time = FrontTimestamp });
                            }

                            sql = @"insert into app_voucher_sort(app_voucher_id, show_time, sortweight) 
                                values(@app_voucher_id, @show_time, @sortweight)";
                            JArray jArray = JArray.Parse(dataInfo.SortData);
                            for (int i = 0; i < jArray.Count; i++)
                            {
                                JObject jObject = JObject.Parse(jArray[i].ToString());
                                returnData = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                                {
                                    app_voucher_id = (int)jObject["app_voucher_id"],
                                    show_time = FrontTimestamp,
                                    sortweight = i + 1,
                                });
                            }
                            break;

                        case "delete":
                            sql = @"update app_voucher set deleted = @status where app_voucher_id = @app_voucher_id";
                            returnData = await conn.QueryAsync(sql, new
                            {
                                status = dataInfo.Status,
                                app_voucher_id = json.VoucherId,
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
        public async Task<IActionResult> Delete(FrontEndData json)
        {
            string sql = "";
            var dataInfo = JsonConvert.DeserializeObject<VoucherData>(json.DataInfo);
            dynamic returnData = "Success";

            try
            {
                using (var conn = _context.CreateConnection())
                {
                    sql = @"UPDATE app_voucher SET deleted = @deleted where app_voucher_id = @app_voucher_id";

                    await conn.QueryAsync(sql, new { deleted = dataInfo.Status, app_voucher_id = json.VoucherId });
                }
                return Ok(returnData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        public async Task<string> GetVoucherList()
        {

            string postData = "{\"categoryID\":\"\"}";
            string apiUrl = "https://middlePlatform.jrgtw.com/FULIAPIController/listItems"; // 請修改為你的 API 網址
            string apiKey = "BfGrzr2yQEWcW1QwJp5SuzK2TVlZvzDO"; // 請修改為你的 API 金鑰

            using (var httpClient = new HttpClient())
            {
                // 設定 headers 中的 apikey 參數
                httpClient.DefaultRequestHeaders.Add("apikey", apiKey);

                // 設定要 POST 的內容
                var content = new StringContent(postData, Encoding.UTF8, "application/json");

                // 發送 POST 請求
                var response = await httpClient.PostAsync(apiUrl, content);

                // 檢查請求是否成功
                if (response.IsSuccessStatusCode)
                {
                    // 請求成功，處理回應資料
                    string responseBody = await response.Content.ReadAsStringAsync();

                    return responseBody;
                }
                else
                {
                    // 請求失敗，處理錯誤
                    return "{\"error\":\"" + response.StatusCode.ToString() + "\"}";
                }
            }
        }
    }

}

public class VoucherData
{
    public int? Status { get; set; }
    public string? Title { get; set; }
    public string? Sub_title { get; set; }
    public string[]? Time { get; set; }
    public string? Push_title { get; set; }
    public string? Activity_image { get; set; }
    public string? CreateDate { get; set; }
    public string? Image_url { get; set; }
    public string? SortData { get; set; }


    public string? Category_id { get; set; }
    public string? Category_name { get; set; }
    public string? Group_name { get; set; }
    public string? Group_id { get; set; }
    public string? GroupDesc { get; set; }
}
