using Dapper;
using Microsoft.AspNetCore.Mvc;
using PKApp.DIObject;
using System.Data;

namespace PKApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BusinessController : ControllerBase
    {
        private readonly DapperContext _context;

        public BusinessController(DapperContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetBusinessData()
        {
            try
            {
                using (var conn = _context.CreateConnection())
                {
                    string sql = @"Select * From BusinessData Where Type = '保留'";
                    var BusinessData = await conn.QueryAsync(sql);
                    return Ok(BusinessData.ToList());
                }
            }
            catch (Exception ex)
            {
                using (var conn = _context.CreateConnection())
                {
                    string sql = @"Insert Into SystemLog(PageName, PageFunc, PageLog, EX) 
                                        Values(@PageName, @PageFunc, @PageLog, @EX)";

                    var parameters = new DynamicParameters();
                    parameters.Add("PageName", "BusinessController", DbType.String);
                    parameters.Add("PageFunc", "GetBusinessData", DbType.String);
                    parameters.Add("PageLog", "Get Business Data", DbType.String);
                    parameters.Add("Ex", ex.Message, DbType.String);

                    var id = await conn.QuerySingleAsync<int>(sql, parameters);

                    return StatusCode(500, ex.Message);
                }

            }
        }
    }
}
