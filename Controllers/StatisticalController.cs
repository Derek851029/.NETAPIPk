using Dapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using PKApp.ConfigOptions;
using PKApp.DIObject;
using PKApp.Services;
using PKApp.Tools;
using System.Data;

namespace PKApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticalController : ControllerBase
    {
        private readonly ILogger<StatisticalController> _logger;
        private readonly DapperContext _context;
        private readonly IStatisticalService _statisticalService;
        private readonly DynamicTool _dynamicTool;

        public StatisticalController(ILogger<StatisticalController> logger, DapperContext context,
            IStatisticalService statisticalService, DynamicTool dynamicTool)
        {
            _logger = logger;
            _context = context;
            _statisticalService = statisticalService;
            _dynamicTool = dynamicTool;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string value, System.DateTime start, System.DateTime end, string brand,
           int page, int pageSize)
        {
            try
            {
                string sql = "";
                dynamic returnData = null;
                DateTimeOffset zeroFrontDay = new DateTimeOffset();
                DateTimeOffset endFrontDay = new DateTimeOffset();

                Int32 startTime = 0;
                Int32 endTime = 0;

                using (var conn = _context.CreateConnection())
                {
                    switch (value)
                    {
                        case "memberList":
                            sql = @"SELECT *, (SELECT COUNT(*) FROM member) AS TotalCount FROM member member
                                    LEFT JOIN member_statistical_data statistical on member.mid = statistical.mid
                                    WHERE statistical.mid IS NOT NULL
                                    LIMIT @LIMIT OFFSET @OFFSET"
                            ;

                            var listData = await conn.QueryAsync(sql, new { LIMIT = pageSize, OFFSET = (page - 1) * pageSize });
                            returnData = HandleListData(listData);

                            break;
                        case "newsReadMonth":
                            zeroFrontDay = new System.DateTime(start.Year, start.Month, start.Day);
                            endFrontDay = new System.DateTime(end.Year, end.Month, end.Day, 23, 59, 59);

                            startTime = (Int32)zeroFrontDay.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                            endTime = (Int32)endFrontDay.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                            brand = brand == "ALL" ? "" : brand;

                            returnData = await _statisticalService.HandleDB(value, startTime, endTime, brand);
                            break;

                        case "downloadYear":
                            zeroFrontDay = new System.DateTime(start.Year, start.Month, start.Day);
                            endFrontDay = new System.DateTime(end.Year, end.Month, end.Day, 23, 59, 59);

                            startTime = (Int32)zeroFrontDay.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                            endTime = (Int32)endFrontDay.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();

                            sql = @"SELECT DISTINCT rdate, device, count(*)  AS total FROM
                                    (SELECT a.mid, a.pk_id, b.devicecode_id, c.device, FROM_UNIXTIME(a.created, '%Y/%m/%d') rdate 
                                    FROM member a, member_accesstoken b,api_register_device c 
                                    where a.mid=b.mid and b.devicecode_id=c.devicecode_id and a.created>@startTime and a.created < @endTime) newdownload
                                    group by rdate, device";
                            var notRepeatDownload = await conn.QueryAsync(sql, new { startTime = startTime, endTime = endTime });
                            sql = @"SELECT DISTINCT rdate,device, count(*)  AS total FROM
                                    (SELECT FROM_UNIXTIME(created, '%Y/%m/%d') rdate,device FROM api_register_device  
                                    where created>@startTime and created < @endTime) newdownload
                                    group by rdate, device";
                            var repeatDownload = await conn.QueryAsync(sql, new { startTime = startTime, endTime = endTime });
                            returnData = new
                            {
                                notRepeat = notRepeatDownload,
                                repeat = repeatDownload,
                            };
                            break;

                        case "dynamicReport":
                            //var dyncmicData = await _dynamicTool.GetDynamic(linkType);
                            _statisticalService.DynamicReport();
                            break;

                        case "activityList":
                            sql = @"SELECT * FROM activity";

                            using (var actConn = _context.CreateConnection2())
                            {
                                returnData = await actConn.QueryAsync(sql);
                            }
                            break;
                        case "gameplayer":
                            zeroFrontDay = new System.DateTime(start.Year, start.Month, start.Day);
                            endFrontDay = new System.DateTime(end.Year, end.Month, end.Day, 23, 59, 59);

                            startTime = (Int32)zeroFrontDay.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                            endTime = (Int32)endFrontDay.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                            sql = @"SELECT app_activity_id, from_unixtime(created, '%Y/%m/%d') AS time, count(*) AS total
                                    FROM logs_participate_activity  
                                    where created>@startTime and a.created < @endTime group by from_unixtime(created, '%Y-%m-%d')
                                    order by from_unixtime(created, '%Y/%m/%d') desc";
                            string aSql = @"SELECT * FROM activity";
                            var rlcms = await conn.QueryAsync(sql, new { startTime = startTime, endTime = endTime });
                            List<dynamic> activity;

                            using (var actConn = _context.CreateConnection2())
                            {
                                var actData = await actConn.QueryAsync(aSql);
                                activity = actData.ToList();
                            }

                            foreach (var obj in rlcms.ToList())
                            {
                                var data = activity.Find(item => item.activity_id == obj.app_activity_id);
                                if (data != null)
                                {
                                    rlcms.ToList().Add(data.title);
                                }
                            }
                            returnData = rlcms;

                            break;

                            //case "storeMonthKFC":
                            //    sql = @"SELECT *,SUM(event.map_order_count) AS map_order,SUM(event.map_delivery_count) AS map_delivery,SUM(event.navigation_count) AS navigation FROM report_store_event event
                            //            LEFT JOIN app_store store on event.app_store_id = store.app_store_id
                            //            where event.year = @year and store.brand = 'Kfc'
                            //            group by event.year,event.month,event.app_store_id";
                            //    var storeMonthData = await conn.QueryAsync(sql, new { year = year });
                            //    returnData = HandleStoreMonthData(storeMonthData);
                            //    break;
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

        [HttpPost("excel")]
        public IActionResult ExportToExcel(FrontEndData json)
        {
            JArray jsonArray = JArray.Parse(json.DataInfo);
            dynamic Columns = new { };
            string fileNmae = "";
            DataTable dataTable = new DataTable();
            try
            {
                switch (json.Type)
                {
                    case "downloadYear":
                        Columns = new
                        {
                            rdate = "日期",
                            device = "裝置",
                            total = "下載次數",
                        };
                        fileNmae = "APP下載次數報表.xlsx";
                        dataTable = DataTableHelper.ToDataTable(jsonArray, Columns);
                        break;
                    case "newsReadMonth":
                        Columns = new
                        {
                            id = "ID",
                            title = "活動名稱",
                            member = "總發送會員數(不重複)",
                            send = "成功發送數(不重複)",
                            open = "開啟數(不重複)",
                            percent = "平均開啟率",
                            click = "點擊數(不重複)",
                        };
                        fileNmae = "最新消息查看率統計.xlsx";
                        dataTable = DataTableHelper.NewsToDataTable(jsonArray, Columns);
                        break;
                }


                byte[] excelBytes = ExportExcel(dataTable, fileNmae);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileNmae);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }

        }

        public IEnumerable<dynamic> HandleListData(IEnumerable<dynamic> data)
        {
            try
            {
                List<dynamic> updatedData = new List<dynamic>();

                foreach (var item in data.ToList())
                {
                    JObject jobject = JObject.Parse(item.read_news_statistics);
                    int totalCount = 0;
                    double totalParsent = 0;

                    foreach (JProperty property in jobject.Properties())
                    {
                        totalCount += 1;
                        if (property.Value != null)
                        {
                            if (property.Value.ToString() != "NaN")
                            {
                                double value = (double)property.Value;
                                totalParsent += value;
                            }
                        }

                    }


                    double average = Math.Round(totalParsent / totalCount * 100, 4);
                    item.read_news_statistics = average.ToString();
                    updatedData.Add(item);
                }

                return updatedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }

        }

        public IEnumerable<dynamic> HandleStoreMonthData(IEnumerable<dynamic> data)
        {
            List<dynamic> updatedData = new List<dynamic>();

            foreach (var item in data.ToList())
            {
                int index = updatedData.FindIndex(newItem => item.app_store_id == newItem.app_store_id);

                if (index > -1)
                {

                }
                else
                {
                    int month = item.month;

                    switch (month)
                    {
                        case 1: item.one = item.month; break;
                        case 2: item.two = item.month; break;
                        case 3: item.three = item.month; break;
                        case 4: item.four = item.month; break;
                        case 5: item.five = item.month; break;
                        case 6: item.six = item.month; break;
                        case 7: item.seven = item.month; break;
                        case 8: item.eight = item.month; break;
                        case 9: item.nine = item.month; break;
                        case 10: item.ten = item.month; break;
                        case 11: item.eleven = item.month; break;
                        case 12: item.twelve = item.month; break;
                    }

                    item.Remove("month");
                    updatedData.Add(item);
                }

            }

            return updatedData;
        }

        private byte[] ExportExcel(DataTable dataTable, string fileName)
        {
            var sheetName = fileName.Split(".")[0];
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            // 创建 Excel 包
            using (ExcelPackage package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add(sheetName);
                worksheet.Cells["A1"].LoadFromDataTable(dataTable, true);

                for (int col = 1; col <= dataTable.Columns.Count; col++)
                {
                    worksheet.Column(col).Width = 20;
                }

                return package.GetAsByteArray();
            }
        }
    }
}

public class ExportData
{
    public string? Rdate { get; set; }
    public string? Device { get; set; }
    public string? Total { get; set; }
}

public class GamePlayerData
{
    public int? App_activity_id { get; set; }
    public string? Time { get; set; }
    public int? Total { get; set; }
}
