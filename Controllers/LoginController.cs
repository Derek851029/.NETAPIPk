using Dapper;
using Google.Api.Gax.ResourceNames;
using Google.Cloud.RecaptchaEnterprise.V1;
using Microsoft.AspNetCore.Mvc;
using PKApp.DIObject;
using System.Security.Cryptography;
using System.Text;

namespace PKApp.Controllers
{
    public class UserData
    {
        public string AgentID { get; set; }
        public string Token { get; set; }
    }

    public class LoginObj
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string GoogleToken { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly DapperContext _context;
        private readonly JwtHelpers jwt;
        public LoginController(DapperContext context, JwtHelpers jwt)
        {
            _context = context;
            this.jwt = jwt;
        }

        [HttpGet]
        public async Task<IActionResult> GetStaff()
        {
            try
            {
                using (var conn = _context.CreateConnection())
                {
                    string sql = @"SELECT * FROM administrators a LEFT JOIN administrators_profile b on a.aid = b.aid";

                    var user = await conn.QueryAsync(sql);

                    CookieOptions options = new CookieOptions();
                    options.Expires = System.DateTime.Now.AddSeconds(7200);
                    HttpContext.Response.Cookies.Append("AccountAid", "111", options);
                    HttpContext.Response.Cookies.Append("Token", "123", options);


                    return Ok(user.ToList());
                }
            }
            catch (Exception ex)
            {
                using (var conn = _context.CreateConnection())
                {
                    //string sql = @"Insert Into SystemLog(PageName, PageFunc, PageLog, EX) 
                    //                    Values(@PageName, @PageFunc, @PageLog, @EX)";

                    //var parameters = new DynamicParameters();
                    //parameters.Add("PageName", "MenuController", DbType.String);
                    //parameters.Add("PageFunc", "GetMenus", DbType.String);
                    //parameters.Add("PageLog", "Get menu list", DbType.String);
                    //parameters.Add("Ex", ex.Message.ToString(), DbType.String);

                    //await conn.ExecuteAsync(sql, parameters);

                    return StatusCode(500, ex.Message);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetUserData(LoginObj data)
        {
            try
            {
                dynamic returnData = "";

                using (var conn = _context.CreateConnection())
                {
                    string sql = @"Select * From administrators a LEFT JOIN administrators_profile b on a.aid = b.aid Where account = @account";
                    var acnt = await conn.QueryAsync(sql, new { account = data.Username });
                    if (acnt.Any())
                    {
                        if (acnt.Single().security.Contains("$"))
                        {
                            returnData = new
                            {
                                data = acnt.ToList(),
                                type = "Change"
                            };
                        }
                        else
                        {
                            sql = @"Select * From administrators Where security = @security AND account = @account";
                            var ps = await conn.QueryAsync(sql, new { security = MD5Hash(data.Password), account = data.Username });
                            if (ps.Any())
                            {
                                returnData = new
                                {
                                    data = acnt.ToList(),
                                    type = "Success"
                                };
                            }
                            else
                            {
                                returnData = new
                                {
                                    data = acnt.ToList(),
                                    type = "Not Found"
                                };

                            }
                        }

                    }
                    else
                    {
                        returnData = new
                        {
                            data = acnt.ToList(),
                            type = "Not Found"
                        };
                    }

                    if (returnData.type == "Success")
                    {
                        //Boolean check = CheckRecaptcha(data.GoogleToken);
                        //if (check)
                        //{
                        //    var token = jwt.GenerateToken(data.Username);
                        //    var aid = acnt.Single().aid;
                        //    //var userinfo = new UserData() { AgentID = user.ToList()[1], Token = token };

                        //    CookieOptions options = new CookieOptions();
                        //    options.Expires = System.DateTime.Now.AddSeconds(7200);
                        //    HttpContext.Response.Cookies.Append("AccountAid", acnt.Single().aid.ToString());
                        //    HttpContext.Response.Cookies.Append("Account", acnt.Single().account.ToString());
                        //    HttpContext.Response.Cookies.Append("Name", acnt.Single().name.ToString());
                        //    HttpContext.Response.Cookies.Append("Token", token, options);
                        //}
                        //else
                        //{
                        //    returnData = new
                        //    {
                        //        data = acnt.ToList(),
                        //        type = "Recaptcha Fail"
                        //    };
                        //}

                        var token = jwt.GenerateToken(data.Username);
                        var aid = acnt.Single().aid;
                        //var userinfo = new UserData() { AgentID = user.ToList()[1], Token = token };

                        CookieOptions options = new CookieOptions();
                        options.Expires = System.DateTime.Now.AddSeconds(7200);
                        HttpContext.Response.Cookies.Append("AccountAid", acnt.Single().aid.ToString());
                        HttpContext.Response.Cookies.Append("Account", acnt.Single().account.ToString());
                        HttpContext.Response.Cookies.Append("Name", acnt.Single().name.ToString());
                        HttpContext.Response.Cookies.Append("Token", token, options);
                    }


                    return Ok(returnData);
                }
            }
            catch (Exception ex)
            {
                using (var conn = _context.CreateConnection())
                {

                    return StatusCode(500, ex.Message);
                }
            }
        }

        [HttpPost("resetToken")]
        public IActionResult ResetToken()
        {
            string accountValue = HttpContext.Request.Cookies["Account"];
            var token = jwt.GenerateToken(accountValue);
            CookieOptions options = new CookieOptions();
            options.Expires = System.DateTime.Now.AddSeconds(7200);
            HttpContext.Response.Cookies.Append("Token", token, options);

            return Ok("Success");
        }

        public static string MD5Hash(string input)
        {
            using (var md5 = MD5.Create())
            {
                var result = md5.ComputeHash(Encoding.ASCII.GetBytes(input));
                var strResult = BitConverter.ToString(result);
                return strResult.Replace("-", "");
            }
        }

        public static Boolean CheckRecaptcha(string token)
        {
            RecaptchaEnterpriseServiceClient client = RecaptchaEnterpriseServiceClient.Create();
            ProjectName projectName = new ProjectName("jrg-service23-pkapp-prod");
            CreateAssessmentRequest createAssessmentRequest = new CreateAssessmentRequest()
            {
                Assessment = new Assessment()
                {
                    // Set the properties of the event to be tracked.
                    Event = new Event()
                    {
                        SiteKey = "6LeOp90nAAAAAPLJz2H1qUAUXk7rusPRwI1Knl-w",
                        Token = token,
                        ExpectedAction = "LOGIN"
                    },
                },
                ParentAsProjectName = projectName
            };
            Assessment response = client.CreateAssessment(createAssessmentRequest);
            if (response.TokenProperties.Valid == false)
            {
                return false;
            }
            if (response.TokenProperties.Action != "LOGIN")
            {

                return false;
            }

            if (response.RiskAnalysis.Score <= 0.1)
            {
                return false;
            }
            return true;
        }
    }
}
