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
    public class PaymentController : ControllerBase
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly DapperContext _context;
        private readonly IFilesService _filesService;

        public PaymentController(ILogger<PaymentController> logger, DapperContext context, IFilesService filesService)
        {
            _logger = logger;
            _context = context;
            _filesService = filesService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                string sql = @"SELECT * FROM app_payment";
                IEnumerable<dynamic> returnData = null;

                using (var conn = _context.CreateConnection())
                {
                    returnData = await conn.QueryAsync(sql);
                }

                return Ok(returnData);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post(FrontEndData json)
        {
            try
            {
                dynamic returnData = "Success";

                long todayUnix = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                var userAid = HttpContext.Request.Cookies["AccountAid"];

                string sql = @"UPDATE app_payment SET status = @status, changed = @changed,  changed_aid = @changed_aid WHERE payment_key = @payment_key";
                var dataInfo = JsonConvert.DeserializeObject<PaymentData>(json.DataInfo);

                using (var conn = _context.CreateConnection())
                {
                    await conn.QueryAsync(sql, new
                    {
                        status = dataInfo.Status,
                        changed = todayUnix,
                        changed_aid = userAid,
                        payment_key = dataInfo.Payment_key
                    });
                }

                return Ok(returnData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }
    }
}

public class PaymentData
{
    public string? Payment_key { get; set; }
    public int? Status { get; set; }
}
