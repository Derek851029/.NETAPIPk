using Dapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PKApp.ConfigOptions;
using PKApp.DIObject;
using System.Security.Cryptography;
using System.Text;

namespace PKApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdministratorController : ControllerBase
    {
        private readonly ILogger<AdministratorController> _logger;
        private readonly DapperContext _context;

        public AdministratorController(DapperContext context, ILogger<AdministratorController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAdministrator(int id)
        {
            try
            {
                string sql = "";
                using (var conn = _context.CreateConnection())
                {
                    if (id == 0)
                    {
                        sql = @"select b.*, a.*, d.*, c.rid as rid2, (GROUP_CONCAT(f.role_title)) as rolesText from pkcard_rlcms.administrators as a
                                inner join pkcard_rlcms.account as b on a.aid = b.aid
                                left join(select * from pkcard_rlcms.administrators_roles where rid = 1) as c on a.aid = c.aid
                                left join pkcard_rlcms.administrators_profile as d on a.aid = d.aid
                                left join pkcard_rlcms.administrators_roles as e on a.aid = e.aid
                                left join pkcard_rlcms.administrators_roles_manage as f on e.rid = f.rid
                                where b.deleted = 0 and 
                                c.rid IS NULL and 
                                a.aid > 1
                                group by a.aid
                                order by b.created desc";

                        var userList = await conn.QueryAsync(sql);
                        return Ok(userList.ToList());
                    }
                    else
                    {
                        sql = @"select *,GROUP_CONCAT(rid) as group_rid from account a
                                left join  administrators b on a.aid = b.aid
                                left join administrators_profile c on a.aid = c.aid
                                left join administrators_roles d on a.aid = d.aid
                                where a.aid = @id
                                group by a.aid";
                        var userData = await conn.QueryAsync(sql, new { id = id });
                        return Ok(userData.ToList());
                    }

                }
            }
            catch (Exception ex)
            {
                using (var conn = _context.CreateConnection())
                {
                    _logger.LogError(ex.Message);
                    return StatusCode(500, ex.Message);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> NewAdministrator(FrontEndData json)
        {
            try
            {
                var dataInfo = JsonConvert.DeserializeObject<AdministratorData>(json.DataInfo);
                string type = json.Type;
                string sql = "";
                dynamic returnData = "Success";

                using (var conn = _context.CreateConnection())
                {
                    switch (type)
                    {
                        case "new":
                            sql = @"insert into account(status, deleted, created, modify, usetime) 
                                    values(@status, @deleted, @created, @modify, @usetime);
                                    SELECT CAST(LAST_INSERT_ID() AS UNSIGNED INTEGER)";

                            var aid = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                            {
                                status = 1,
                                deleted = 0,
                                created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                modify = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                usetime = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                            });

                            sql = @"insert into administrators(aid, account, security) values(@aid, @account, @security)";
                            await conn.QueryAsync(sql, new { aid = aid, account = dataInfo.Account, security = MD5Hash(dataInfo.Password) });

                            sql = @"insert into administrators_profile(aid, name, phone,email) values(@aid, @name,@phone,@email)";
                            await conn.QueryAsync(sql, new { aid = aid, name = dataInfo.Name, phone = dataInfo.Phone ?? "", email = dataInfo.Email ?? "", });

                            sql = @"insert into administrators_roles(aid, rid) values(@aid,@rid)";
                            foreach (var rid in dataInfo.Roles)
                            {
                                await conn.QueryAsync(sql, new { aid = aid, rid = rid });
                            }
                            break;

                        case "edit":
                            sql = @"update account set modify = @modify where aid = @aid";
                            await conn.QueryAsync(sql, new
                            {
                                modify = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                                aid = json.Aid,
                            });

                            sql = @"update administrators_profile set name = @name, phone = @phone, email = @email where aid = @aid";
                            await conn.QueryAsync(sql, new { name = dataInfo.Name, phone = dataInfo.Phone ?? "", email = dataInfo.Email ?? "", aid = json.Aid });

                            sql = @"delete from administrators_roles where aid = @aid";
                            await conn.QueryAsync(sql, new { aid = json.Aid });

                            sql = @"insert into administrators_roles(aid, rid) values(@aid,@rid)";
                            foreach (var rid in dataInfo.Roles)
                            {
                                await conn.QueryAsync(sql, new { aid = json.Aid, rid = rid });
                            }
                            break;

                        case "update":
                            sql = @"UPDATE administrators SET security = @security WHERE aid = @aid";
                            await conn.QueryAsync(sql, new { security = MD5Hash(dataInfo.Password), aid = json.Aid });
                            break;

                        case "delete":
                            sql = @"update account set deleted = 1 where aid = @aid";
                            await conn.QueryAsync(sql, new { aid = json.Aid });
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

        public static string MD5Hash(string input)
        {
            using (var md5 = MD5.Create())
            {
                var result = md5.ComputeHash(Encoding.ASCII.GetBytes(input));
                var strResult = BitConverter.ToString(result);
                return strResult.Replace("-", "");
            }
        }
    }
}

public class AdministratorData
{
    public int? Status { get; set; }
    public string? Account { get; set; }
    public string? Password { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string[]? Roles { get; set; }
    public string? CreateDate { get; set; }
}

