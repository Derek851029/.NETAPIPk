using Dapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PKApp.DIObject;
using System.Data;

public class CaseData
{
    public string dataInfo { get; set; }
    public string systemProduct { get; set; }
}

public class DataInfo
{
    public string? assistCompany { get; set; }
    public string? contactName { get; set; }
    public string? contactPhone { get; set; }
    public string? content { get; set; }
    public string? cusID { get; set; }
    public string? custName { get; set; }
    public string[]? owner { get; set; }
    public string? projectName { get; set; }
    public string? remark { get; set; }
}

public class OEProudctData
{
    public int? key { get; set; }
    public string? name { get; set; }
    public int? quantity { get; set; }
    public int? realPrice { get; set; }
    public int? referPrice { get; set; }
}

namespace PKApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CaseController : ControllerBase
    {
        private readonly DapperContext _context;

        public CaseController(DapperContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCase()
        {
            try
            {
                using (var conn = _context.CreateConnection())
                {
                    string sql = @"Select a.*, b.BUSINESSNAME From CaseList a Left Join BusinessData b on a.ClinetID = b.ID";

                    var cases = await conn.QueryAsync(sql);
                    return Ok(cases.ToList());
                }
            }
            catch (Exception ex)
            {
                using (var conn = _context.CreateConnection())
                {
                    string sql = @"Insert Into SystemLog(PageName, PageFunc, PageLog, EX) 
                                        Values(@PageName, @PageFunc, @PageLog, @EX)";

                    var parameters = new DynamicParameters();
                    parameters.Add("PageName", "CaseController", DbType.String);
                    parameters.Add("PageFunc", "GetCase", DbType.String);
                    parameters.Add("PageLog", "Get case list", DbType.String);
                    parameters.Add("Ex", ex.Message.ToString(), DbType.String);

                    await conn.ExecuteAsync(sql, parameters);

                    return StatusCode(500, ex.Message);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> newCase(CaseData caseData)
        {
            try
            {
                var dataInfo = JsonConvert.DeserializeObject<DataInfo>(caseData.dataInfo);
                var systemProduct = JsonConvert.DeserializeObject<List<OEProudctData>>(caseData.systemProduct);

                using var conn = _context.CreateConnection();
                string sql = @"Insert Into CaseList([CaseName],[ClinetID],[Contact],[Phone],[Personnel],[AssistCompany],[ProjectContent]
                                        ,[Remark],[Status] ,[creater]) 
                                        Values(@CaseName, @ClinetID, @Contact, @Phone, @Personnel, @AssistCompany, @ProjectContent
                                        , @Remark, @Status, @creater)

                                        SELECT CAST(SCOPE_IDENTITY() as int)";

                var caseID = await conn.QuerySingleAsync<int>(sql, new
                {
                    CaseName = dataInfo.projectName,
                    ClinetID = dataInfo.cusID,
                    Contact = dataInfo.contactName,
                    Phone = dataInfo.contactPhone,
                    Personnel = dataInfo.owner[1],
                    AssistCompany = dataInfo.assistCompany,
                    ProjectContent = dataInfo.content,
                    Remark = dataInfo.remark,
                    Status = 0,
                    creater = HttpContext.Request.Cookies["AgentID"],
                });

                if (systemProduct.Count > 0)
                {
                    foreach (var oEProudctData in systemProduct)
                    {
                        if (oEProudctData.quantity != 0)
                        {
                            sql = @"Insert Into CaseSystemData([caseID],[productID],[productName],[productQuantity],[realPrice],[referPrice])
	                              Values(@caseID, @productID, @productName, @productQuantity, @realPrice, @referPrice)

                                SELECT CAST(SCOPE_IDENTITY() as int)";
                            await conn.ExecuteAsync(sql, new
                            {

                                caseID = caseID,
                                productID = oEProudctData.key,
                                productName = oEProudctData.name,
                                productQuantity = oEProudctData.quantity,
                                realPrice = oEProudctData.realPrice,
                                referPrice = oEProudctData.referPrice,
                            });
                        }
                    }
                }


                return Ok("Success");
            }
            catch (Exception ex)
            {
                using (var conn = _context.CreateConnection())
                {
                    string sql = @"Insert Into SystemLog(PageName, PageFunc, PageLog, EX) 
                                        Values(@PageName, @PageFunc, @PageLog, @EX)";

                    var parameters = new DynamicParameters();
                    parameters.Add("PageName", "CaseController", DbType.String);
                    parameters.Add("PageFunc", "GetCase", DbType.String);
                    parameters.Add("PageLog", "Get case list", DbType.String);
                    parameters.Add("Ex", ex.Message.ToString(), DbType.String);

                    await conn.ExecuteAsync(sql, parameters);

                    return StatusCode(500, ex.Message);
                }
            }
        }
    }
}
