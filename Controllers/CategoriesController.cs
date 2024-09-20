using Dapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PKApp.ConfigOptions;
using PKApp.DIObject;

namespace PKApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly DapperContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(DapperContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
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
                            sql = @"select * from categories where status = 1 and type = 'AppStoreType' order by created desc";
                            returnData = await conn.QueryAsync(sql);
                            break;
                        case "edit":
                            sql = @"select * from categories where cat_id = @cat_id";
                            returnData = await conn.QueryAsync(sql, new { cat_id = id });
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
        public async Task<IActionResult> NewCategories(FrontEndData json)
        {
            try
            {
                string sql = "";
                string type = json.Type;
                var dataInfo = JsonConvert.DeserializeObject<CategoriesData>(json.DataInfo);
                dynamic returnData = "Success";

                using (var conn = _context.CreateConnection())
                {
                    switch (type)
                    {
                        case "new":
                            sql = @"INSERT INTO categories(status,type,title,icon,arg_json,sortweight,changed,changed_aid,created,created_aid)
                                    VALUES(@status,@type,@title,@icon,@arg_json,@sortweight,@changed,@changed_aid,@created,@created_aid);";
                            await conn.QueryAsync(sql, new
                            {
                                status = 1,
                                type = "AppStoreType",
                                title = dataInfo.Title,
                                icon = "",
                                arg_json = "[]",
                                sortweight = 50,
                                changed = 0,
                                changed_aid = 0,
                                created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                created_aid = HttpContext.Request.Cookies["AccountAid"],
                            });
                            break;

                        case "edit":
                            sql = @"update categories set title = @title where cat_id = @cat_id";
                            await conn.QueryAsync(sql, new { title = dataInfo.Title, cat_id = json.StoreTypeID });
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

public class CategoriesData
{
    public int? Cat_id { get; set; }
    public int? Status { get; set; }
    public string Title { get; set; }
    public int? Icon { get; set; }
    public string? Arg_json { get; set; }
    public int? Sortweight { get; set; }
    public int? Changed { get; set; }
    public int? Changed_aid { get; set; }
    public DateTimeOffset? Created { get; set; }
    public int? Created_aid { get; set; }
}
