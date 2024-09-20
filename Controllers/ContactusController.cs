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
    public class ContactusController : ControllerBase
    {
        private readonly ILogger<ContactusController> _logger;
        private readonly DapperContext _context;
        private readonly IFilesService _filesService;

        public ContactusController(ILogger<ContactusController> logger, DapperContext context, IFilesService filesService)
        {
            _logger = logger;
            _context = context;
            _filesService = filesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetActivity(string type, int id)
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
                            sql = @"SELECT contactus.* ,m.pk_id, a.name as admin_name, q.title FROM app_contactus contactus
                                    LEFT JOIN member m on contactus.created_mid = m.mid
                                    LEFT JOIN administrators_profile a on contactus.changed_aid = a.aid
                                    LEFT JOIN app_contactus_question q on contactus.question_type = q.code
                                    WHERE deleted = 0 ORDER by contactus.created desc";

                            returnData = await conn.QueryAsync(sql);
                            break;

                        case "edit":
                            sql = @"SELECT * FROM app_contactus contactus 
                                    LEFT JOIN app_contactus_question question on contactus.question_type = question.code
                                    WHERE app_contactus_id = @app_contactus_id";

                            returnData = await conn.QueryAsync(sql, new { app_contactus_id = id });
                            break;

                        case "editVersion":
                            sql = @"SELECT * FROM app_contactus_version contactus 
                                    LEFT JOIN app_contactus_question question on contactus.question_type = question.code
                                    WHERE app_contactus_id = @app_contactus_id";

                            returnData = await conn.QueryAsync(sql, new { app_contactus_id = id });
                            break;

                        case "version":
                            sql = @"SELECT * FROM app_contactus_version WHERE app_contactus_id = @app_contactus_id order by ver_created desc";

                            returnData = await conn.QueryAsync(sql, new { app_contactus_id = id });
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
        public async Task<IActionResult> Post(FrontEndData json)
        {
            try
            {
                string sql = "";
                string type = json.Type;
                var dataInfo = JsonConvert.DeserializeObject<ContactusData>(json.DataInfo);
                dynamic returnData = "Success";

                long todayUnix = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                var userAid = HttpContext.Request.Cookies["AccountAid"];

                using (var conn = _context.CreateConnection())
                {
                    switch (type)
                    {
                        case "edit":
                            int version = await InsertVersion((int)json.ContactusID);

                            sql = @"UPDATE app_contactus SET reply_content = @reply_content,reply_status = @reply_status,version = @version,
                                    push_title = @push_title, start_push_time = @start_push_time,
                                    changed = @changed,changed_aid = @changed_aid WHERE app_contactus_id = @app_contactus_id;";
                            await conn.QueryAsync(sql, new
                            {
                                version = version + 1,
                                reply_content = dataInfo.Reply_content,
                                reply_status = dataInfo.Reply_status,
                                push_title = "官方已回覆您的問題",
                                start_push_time = todayUnix + 3600,
                                changed = todayUnix,
                                changed_aid = userAid,
                                app_contactus_id = json.ContactusID,
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

        public async Task<int> InsertVersion(int id)
        {
            try
            {
                string sql = "";
                List<string> jsonStr = new List<string>();

                using (var conn = _context.CreateConnection())
                {

                    sql = @"INSERT INTO app_contactus_version(app_contactus_id,version,status,deleted,brand,question_type,name,tel,
                            image_fid, content, reply_content, reply_status, push_title, device_system, device_version, device_brand,
                            device_model,email_send_time, ver_created,ver_created_aid)
                            SELECT app_contactus_id AS app_contactus_id, version AS version, status AS status, deleted AS deleted, brand AS brand, question_type AS question_type, 
                            name AS name, tel AS tel, image_fid AS image_fid, content AS content, reply_content AS reply_content, reply_status AS reply_status,
                            push_title AS push_title, device_system AS device_system, device_version AS device_version, device_brand AS device_brand,
                            device_model AS device_model, email_send_time AS email_send_time, @ver_created AS ver_created, @ver_created_aid AS ver_created_aid FROM app_contactus WHERE app_contactus_id = @app_contactus_id";

                    await conn.QueryAsync(sql, new
                    {
                        app_contactus_id = id,
                        ver_created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                        ver_created_aid = HttpContext.Request.Cookies["AccountAid"],
                    });

                    sql = @"SELECT version FROM app_contactus WHERE app_contactus_id = @app_contactus_id";

                    var getVersion = await conn.QuerySingleOrDefaultAsync(sql, new { app_contactus_id = id });

                    return (int)getVersion.version;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return 0;
            }

        }
    }
}

public class ContactusData
{
    public string? Reply_content { get; set; }
    public int? Reply_status { get; set; }
}
