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
    public class StoreController : ControllerBase
    {
        private readonly DapperContext _context;
        private readonly ILogger _logger;
        private readonly IFirebaseService _firebaseService;

        public StoreController(DapperContext context, ILogger<StoreController> logger, IFirebaseService service)
        {
            _context = context;
            _logger = logger;
            _firebaseService = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetData(string type, int id)
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
                            sql = @"select a.*,b.dynamic_link,c.title as store_type_title from app_store a 
                                    left join dynamic_link b on a.app_store_id =  json_extract(b.other_info,'$.app_store_id')
                                    left join categories c on REPLACE(REPLACE(REPLACE(a.store_type_json, ']', ''), '[', ''), '""', '') = c.cat_id
                                    where a.deleted = 0 and b.link_type = 'store' and b.action = 'toNothing' and c.type = 'AppStoreType' and c.status = 1 
                                    group by app_store_id 
                                    order by a.created desc";
                            returnData = await conn.QueryAsync(sql);
                            break;
                        case "edit":
                            sql = @"SELECT * FROM app_store WHERE app_store_id = @app_store_id";
                            returnData = await conn.QueryAsync(sql, new
                            {
                                app_store_id = id
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
        public async Task<IActionResult> NewStore(FrontEndData json)
        {
            try
            {
                string sql = "";
                string type = json.Type;
                var dataInfo = JsonConvert.DeserializeObject<StoreData>(json.DataInfo);
                dynamic returnData = "Success";
                int image_fid = 0;

                using (var conn = _context.CreateConnection())
                {
                    switch (type)
                    {
                        case "new":
                            sql = @"INSERT INTO pkcard_rlcms.app_store
                                    (version,status,deleted,brand,store_code,store_name,store_phone,
                                    store_addr,store_addr_desc,store_seat,open_times_json,store_type_json,
                                    store_service_json,order_link_json,link_url,link_mate_json,link2_url,
                                    link2_mate_json,longitude,latitude,image_fid,changed,changed_aid,created,created_aid)
                                    VALUES
                                    (@version,@status,@deleted,@brand,@store_code,@store_name,@store_phone,
                                    @store_addr,@store_addr_desc,@store_seat,@open_times_json,@store_type_json,
                                    @store_service_json,@order_link_json,@link_url,@link_mate_json,@link2_url,
                                    @link2_mate_json,@longitude,@latitude,@image_fid,@changed,@changed_aid,@created,@created_aid);

                                    SELECT CAST(LAST_INSERT_ID() as UNSIGNED INTEGER);";
                            var app_store_id = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                            {
                                version = 1,
                                status = dataInfo.Status,
                                deleted = 0,
                                brand = dataInfo.Brand,
                                store_code = dataInfo.Store_code,
                                store_name = dataInfo.Store_name,
                                store_phone = dataInfo.Store_phone ?? "",
                                store_addr = dataInfo.Store_addr,
                                store_addr_desc = dataInfo.Store_addr_desc ?? "",
                                store_seat = dataInfo.Store_seat ?? "",
                                open_times_json = dataInfo.Open_times_json,
                                store_type_json = $"[{dataInfo.Store_type_json}]",
                                store_service_json = $"[{dataInfo.Store_service_json}]",
                                order_link_json = "[]",
                                link_url = "",
                                link_mate_json = "",
                                link2_url = "",
                                link2_mate_json = "",
                                longitude = dataInfo.Longitude,
                                latitude = dataInfo.Latitude,
                                image_fid = 0,
                                changed = 0,
                                changed_aid = 0,
                                created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                created_aid = HttpContext.Request.Cookies["AccountAid"],
                            });
                            string link = await CreateDynamicLink(app_store_id, dataInfo.Store_code, dataInfo.Brand.Substring(0, 1));
                            if (link == "")
                            {
                                return StatusCode(500);
                            }
                            break;

                        case "edit":
                            sql = @"UPDATE app_store SET
                                    status = @status,brand = @brand,store_code = @store_code,
                                    store_name = @store_name,store_phone = @store_phone,store_addr = @store_addr,
                                    store_addr_desc = @store_addr_desc,store_seat = @store_seat,open_times_json = @open_times_json,
                                    store_type_json = @store_type_json,store_service_json = @store_service_json,
                                    longitude = @longitude,latitude = @latitude,changed = @changed,changed_aid = @changed_aid 
                                    WHERE app_store_id = @app_store_id;";
                            await conn.QueryAsync(sql, new
                            {
                                app_store_id = json.StoreID,
                                status = dataInfo.Status,
                                brand = dataInfo.Brand,
                                store_code = dataInfo.Store_code,
                                store_name = dataInfo.Store_name,
                                store_phone = dataInfo.Store_phone,
                                store_addr = dataInfo.Store_addr,
                                store_addr_desc = dataInfo.Store_addr_desc,
                                store_seat = dataInfo.Store_seat,
                                open_times_json = dataInfo.Open_times_json,
                                store_type_json = $"[{dataInfo.Store_type_json}]",
                                store_service_json = $"[{dataInfo.Store_service_json}]",
                                longitude = dataInfo.Longitude,
                                latitude = dataInfo.Latitude,
                                changed = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                changed_aid = HttpContext.Request.Cookies["AccountAid"],
                            });
                            break;
                        case "delete":
                            sql = @"update app_store set deleted = 1 where app_store_id = @app_store_id";
                            await conn.QueryAsync(sql, new { app_store_id = json.StoreID });
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

        public async Task<string> CreateDynamicLink(int app_store_id, string storeCode, string brandPrefix)
        {
            string randKey = GenerateRandomKey();
            string source = $"Store_{brandPrefix}";
            var dataDynamicLink = new System.Collections.Specialized.NameValueCollection
            {
                {"action","toRand" },
                {"rand_key",randKey },
            };
            string dynamicLink = await _firebaseService.GenerateDynamicLink(dataDynamicLink, source, true);
            if (dynamicLink == "")
            {
                return "";
            }
            int lastSlashIndex = dynamicLink.LastIndexOf('/');
            var other_info = new
            {
                app_store_id = app_store_id,
                store_code = storeCode,
                source = source
            };

            string sql = @"INSERT INTO pkcard_rlcms.dynamic_link
                        (rand_key,link_type,dynamic_link,link_code,action,action_id,
                        other_info,use_aid,created,modify)
                        VALUES
                        (@rand_key,@link_type,@dynamic_link,@link_code,@action,
                        @action_id,@other_info,@use_aid,@created,@modify);";
            using (var conn = _context.CreateConnection())
            {
                await conn.QueryAsync(sql, new
                {
                    rand_key = randKey,
                    link_type = "store",
                    dynamic_link = dynamicLink,
                    link_code = dynamicLink.Substring(lastSlashIndex + 1),
                    action = "toNothing",
                    action_id = "0",
                    other_info = JsonConvert.SerializeObject(other_info),
                    use_aid = HttpContext.Request.Cookies["AccountAid"],
                    created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                    modify = 0
                });
            }
            return "Success";
        }

        public string GenerateRandomKey()
        {
            bool isHaveKey = true;
            string randomKey = "";
            Random random = new Random();
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            while (isHaveKey)
            {
                randomKey = new string(Enumerable.Repeat(chars, 16)
              .Select(s => s[random.Next(s.Length)]).ToArray());

                using (var conn = _context.CreateConnection())
                {
                    string sql = @"SELECT rand_key FROM dynamic_link WHERE rand_key = @rand_key";
                    var data = conn.Query(sql, new { rand_key = randomKey });
                    if (!data.Any())
                    {
                        isHaveKey = false;
                    }
                }

            }

            return randomKey;

        }
    }
}

public class StoreData
{
    public int? App_store_id { get; set; }
    public int? Version { get; set; }
    public int? Status { get; set; }
    public int? Deleted { get; set; }
    public string? Brand { get; set; }
    public string? Store_code { get; set; }
    public string? Store_name { get; set; }
    public string? Store_phone { get; set; }
    public string? Store_addr { get; set; }
    public string? Store_addr_desc { get; set; }
    public string? Store_seat { get; set; }
    public string? Open_times_json { get; set; }
    public string? Store_type_json { get; set; }
    public string? Store_service_json { get; set; }
    public string? Store_text { get; set; }
    public string? Order_link_json { get; set; }
    public string? Link_url { get; set; }
    public string? Link_mate_json { get; set; }
    public string? Link2_url { get; set; }
    public string? Link2_mate_json { get; set; }
    public string? Longitude { get; set; }
    public string? Latitude { get; set; }
    public int? Image_fid { get; set; }
    public int Changed { get; set; }
    public int Changed_aid { get; set; }
    public int Created { get; set; }
    public int Created_aid { get; set; }
    public string? CreateDate { get; set; }
    public string[]? Mon { get; set; }
    public string[]? Tues { get; set; }
    public string[]? Web { get; set; }
    public string[]? Thur { get; set; }
    public string[]? Fri { get; set; }
    public string[]? Sat { get; set; }
    public string[]? Sun { get; set; }
}

