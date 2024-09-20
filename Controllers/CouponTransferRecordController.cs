using Dapper;
using Microsoft.AspNetCore.Mvc;
using PKApp.DIObject;

namespace PKApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CouponTransferRecordController : ControllerBase
    {
        private readonly DapperContext _context;
        private readonly ILogger _logger;

        public CouponTransferRecordController(DapperContext context, ILogger<CouponTransferRecordController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string type, int current, string id)
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
                            if (id == "0")
                            {
                                sql = @"SELECT *, (SELECT COUNT(*) FROM member_coupon_gift_record) AS TotalCount 
                                    FROM member_coupon_gift_record ORDER BY created desc LIMIT 10 OFFSET @count";
                                returnData = await conn.QueryAsync(sql, new
                                {
                                    count = current
                                });
                            }
                            else
                            {
                                sql = @"SELECT *, (SELECT COUNT(*) FROM member_coupon_gift_record) AS TotalCount 
                                    FROM member_coupon_gift_record WHERE pk_id LIKE @pk_id AND grantee_pk_id LIKE @pk_id 
                                    ORDER BY created desc LIMIT 10 OFFSET @count";
                                returnData = await conn.QueryAsync(sql, new
                                {
                                    count = current,
                                    pk_id = "%" + id + "%"
                                });
                            }

                            break;

                        case "view":
                            sql = @"SELECT * FROM logs_coupon_gift_event WHERE gift_record_id = @gift_record_id";

                            returnData = await conn.QueryAsync(sql, new { gift_record_id = id });
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
