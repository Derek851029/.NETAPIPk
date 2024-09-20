using Dapper;
using Microsoft.AspNetCore.Mvc;
using PKApp.DIObject;
using System.Data;

namespace PKApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VendorController : ControllerBase
    {
        private readonly DapperContext _context;

        public VendorController(DapperContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetVendor()
        {
            try
            {
                using (var conn = _context.CreateConnection())
                {
                    string sql = @"Select * From VendorData Where Type = '保留'";
                    var vendorData = await conn.QueryAsync(sql);
                    return Ok(vendorData.ToList());
                }
            }
            catch (Exception ex)
            {
                using (var conn = _context.CreateConnection())
                {
                    string sql = @"Insert Into SystemLog(PageName, PageFunc, PageLog, EX) 
                                        Values(@PageName, @PageFunc, @PageLog, @EX)";

                    var parameters = new DynamicParameters();
                    parameters.Add("PageName", "VendorController", DbType.String);
                    parameters.Add("PageFunc", "GetVendor", DbType.String);
                    parameters.Add("PageLog", "Get Vendor Data", DbType.String);
                    parameters.Add("Ex", ex.Message, DbType.String);

                    var id = await conn.QuerySingleAsync<int>(sql, parameters);

                    return StatusCode(500, ex.Message);
                }

            }
        }
    }
}
