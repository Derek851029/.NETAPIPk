using Dapper;
using Microsoft.AspNetCore.Mvc;
using PKApp.DIObject;
using PKApp.Services;

namespace PKApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly ILogger<FileController> _logger;
        private readonly DapperContext _context;
        private readonly ICloudStorageService _cloudStorageService;
        private readonly IConfiguration _configuration;
        public FileController(DapperContext context, ILogger<FileController> logger, ICloudStorageService cloudStorageService, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _cloudStorageService = cloudStorageService;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> GetFiles(int fid)
        {
            try
            {
                using (var conn = _context.CreateConnection())
                {
                    string sql = @"select * from files where fid = @fid";
                    var data = await conn.QuerySingleOrDefaultAsync(sql, new { fid = fid });
                    string bucketName = _configuration["GoogleCloudStorageBucketName"];

                    string signedUrl = $"https://storage.googleapis.com/{bucketName}/{data.file_path}";
                    if (!data.file_path.Contains("https"))
                    {
                        data.file_path = signedUrl;
                    }
                    else
                    {
                        int index = data.file_path.IndexOf("?");
                        string newUrl = index != -1 ? data.file_path.Substring(0, index) : data.file_path;

                        data.file_path = newUrl;
                    }



                    return (Ok(data));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> NewFiles([FromForm] FileModel file)
        {
            try
            {
                int fid = 0;
                IFormFile formFile = file.FormFile[0];
                string[] fileNameType = formFile.FileName.Split('.');
                string type = "video/" + fileNameType[1];
                List<Part> contentType = new List<Part>
                {
                    new Part() { File = "mp3", Type = "video" },
                    new Part() { File = "mp4", Type = "video" },
                    new Part() { File = "mov", Type = "video" }
                };
                Part obj = contentType.Find(value => value.File == fileNameType[1]);
                if (contentType.Contains(obj))
                {
                    type = obj.Type + "/" + fileNameType[1];
                }
                var unixTimestamp = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();

                switch (file.Type)
                {
                    case "video":



                        string fileName = unixTimestamp.ToString() + "." + fileNameType[1];

                        string MediaLink = await _cloudStorageService.UploadFileAsync(formFile, fileName);

                        string sql = @"insert into files(file_name, file_category, file_name_original, file_path, file_type, file_arg, file_size,
                               is_public, created ) values(@file_name, @file_category, @file_name_original, @file_path, @file_type, 
                               @file_arg, @file_size, @is_public, @created);

                                SELECT CAST(LAST_INSERT_ID() as UNSIGNED INTEGER);";
                        using (var conn = _context.CreateConnection())
                        {
                            fid = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                            {
                                file_name = fileName,
                                file_category = file.Page,
                                file_name_original = fileNameType[0],
                                file_path = "",
                                file_type = type,
                                file_arg = "",
                                file_size = formFile.Length,
                                is_public = 1,
                                created = unixTimestamp
                            });

                            if (file.Page == "app_banner")
                            {
                                sql = @"update app_banner set video_fid = @video_fid where app_banner_id = @app_banner_id";
                                await conn.QueryAsync(sql, new { video_fid = fid, app_banner_id = file.DataID });
                            }
                        }
                        break;
                }
                return Ok(fid);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }
    }
}

public class Part
{
    public string? File { get; set; }
    public string? Type { get; set; }
}

public class FileModel
{
    public string? Type { get; set; }
    public string? Page { get; set; }
    public int? DataID { get; set; }
    public List<IFormFile> FormFile { get; set; }
}
