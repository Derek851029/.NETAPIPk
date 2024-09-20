using Dapper;
using Newtonsoft.Json.Linq;
using PKApp.DIObject;
using System.Drawing;

namespace PKApp.Services
{
    public interface IFilesService
    {
        Task<dynamic> UploadFilesToGCS(dynamic dataInfo, string type, string page, int id = 0);
    }
    public class FilesService : IFilesService
    {
        private ILogger<FilesService> _logger;
        private readonly DapperContext _context;
        private readonly ICloudStorageService _cloudStorageService;
        private readonly IConfiguration _configuration;

        public FilesService(DapperContext context, ILogger<FilesService> logger, ICloudStorageService cloudStorageService, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _cloudStorageService = cloudStorageService;
            _configuration = configuration;
        }

        public async Task<dynamic> UploadFilesToGCS(dynamic dataInfo, string type, string page, int id)
        {
            int image_fid = 0;
            int icon_fid = 0;
            int cover_fid = 0;
            int video_fid = 0;

            //string config = "GoogleCloudStorageBucketName_dev";
            //string? env = Environment.GetEnvironmentVariable("stage");
            //if (env == null)
            //{
            //    config = "GoogleCloudStorageBucketName_dev";
            //}
            //else
            //{
            //    config = "GoogleCloudStorageBucketName_" + env;
            //}
            string bucketName = _configuration["GoogleCloudStorageBucketName"];

            string gcsFileFolder = "files";
            string gameFolder = "gamefiles";
            string fileNamePath = "";
            try
            {
                switch (type)
                {
                    case "image":
                        string imageData = dataInfo.Activity_image;
                        JArray ActivityjArray = JArray.Parse(imageData);
                        JObject ActivityjObject = JObject.Parse(ActivityjArray[0].ToString());

                        IFormFile activityForm = Base64ToImage(ActivityjObject);

                        image_fid = await InsertDBfiles(ActivityjObject, activityForm.FileName, page);

                        //連接字串路徑
                        fileNamePath = $"{gcsFileFolder}/{image_fid}/{activityForm.FileName}";

                        await InsertDBPicture(activityForm, fileNamePath, image_fid);
                        await UpdateDBfiles(fileNamePath, image_fid);

                        string imageMediaLink = await _cloudStorageService.UploadFileAsync(activityForm, fileNamePath);


                        return image_fid;

                    case "icon":
                        JArray AppjArray = JArray.Parse(dataInfo.App_image);
                        JObject AppjObject = JObject.Parse(AppjArray[0].ToString());

                        IFormFile appForm = Base64ToImage(AppjObject);

                        icon_fid = await InsertDBfiles(AppjObject, appForm.FileName, page);

                        fileNamePath = $"{gcsFileFolder}/{icon_fid}/{appForm.FileName}";

                        await InsertDBPicture(appForm, fileNamePath, icon_fid);
                        await UpdateDBfiles(fileNamePath, icon_fid);

                        string iconMediaLink = await _cloudStorageService.UploadFileAsync(appForm, fileNamePath);

                        return icon_fid;

                    case "cover":
                        JArray coverjArray = JArray.Parse(dataInfo.Cover_image);
                        JObject coverjObject = JObject.Parse(coverjArray[0].ToString());

                        IFormFile coverForm = Base64ToImage(coverjObject);

                        cover_fid = await InsertDBfiles(coverjObject, coverForm.FileName, page);

                        fileNamePath = $"{gcsFileFolder}/{cover_fid}/{coverForm.FileName}";

                        await InsertDBPicture(coverForm, fileNamePath, cover_fid);
                        await UpdateDBfiles(fileNamePath, cover_fid);

                        string coverMediaLink = await _cloudStorageService.UploadFileAsync(coverForm, fileNamePath);
                        return cover_fid;

                    case "video":
                        JArray VideojArray = JArray.Parse(dataInfo.Video);
                        JObject VideojObject = JObject.Parse(VideojArray[0].ToString());

                        IFormFile VideoForm = Base64ToImage(VideojObject);

                        video_fid = await InsertDBfiles(VideojObject, VideoForm.FileName, page);

                        fileNamePath = $"{gcsFileFolder}/{video_fid}/{VideoForm.FileName}";

                        await InsertDBPicture(VideoForm, fileNamePath, video_fid);
                        await UpdateDBfiles(fileNamePath, video_fid);

                        string videoMediaLink = await _cloudStorageService.UploadFileAsync(dataInfo.Video, fileNamePath);

                        video_fid = await InsertDBfiles(VideojObject, VideoForm.FileName, page);
                        return video_fid;

                    case "webactivity":
                        //這邊dataInfo直接傳image
                        JArray webActivityjArray = JArray.Parse(dataInfo);
                        JObject webActivityjObject = JObject.Parse(webActivityjArray[0].ToString());

                        IFormFile webActivityForm = Base64ToImage(webActivityjObject);

                        image_fid = await InsertDBfiles(webActivityjObject, webActivityForm.FileName, type);

                        if (page == "item_image")
                        {
                            fileNamePath = $"pkgameSheet/{id}/image/reelsheet.png";
                        }
                        else
                        {
                            //連接字串路徑
                            fileNamePath = $"{gameFolder}/{image_fid}/{webActivityForm.FileName}";
                        }

                        await InsertDBPicture(webActivityForm, fileNamePath, image_fid);
                        await UpdateDBfiles(fileNamePath, image_fid);

                        string activityMediaLink = await _cloudStorageService.UploadFileAsync(webActivityForm, fileNamePath);
                        string singleUrl = $"https://storage.googleapis.com/{bucketName}/{fileNamePath}";


                        return singleUrl;
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
        }

        private async Task<int> InsertDBfiles(JObject jObject, string newFileNmae, string page)
        {
            try
            {
                var unixTimestamp = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                string file_name_original = (string)jObject["name"];
                int size = (int)jObject["size"];
                string type = (string)(jObject["type"]);

                string sql = @"insert into files(file_name, file_category, file_name_original, file_path, file_type, file_arg, file_size,
                               is_public, created ) values(@file_name, @file_category, @file_name_original, @file_path, @file_type, 
                               @file_arg, @file_size, @is_public, @created);

                                SELECT CAST(LAST_INSERT_ID() as UNSIGNED INTEGER);";
                using (var conn = _context.CreateConnection())
                {
                    var fid = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                    {
                        file_name = newFileNmae,
                        file_category = page,
                        file_name_original = file_name_original,
                        file_path = "",
                        file_type = type,
                        file_arg = "",
                        file_size = size,
                        is_public = 1,
                        created = unixTimestamp
                    });
                    return fid;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return 0;
            }

        }

        private async Task UpdateDBfiles(string fileNamePath, int fid)
        {
            try
            {
                string sql = @"UPDATE files SET file_path = @file_path WHERE fid = @fid";
                using (var conn = _context.CreateConnection())
                {
                    await conn.QueryAsync(sql, new
                    {
                        file_path = fileNamePath,
                        fid = fid,
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        private async Task<int> InsertDBPicture(IFormFile IFormFile, string fileNamePath, int fid)
        {
            try
            {
                var unixTimestamp = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                (int width, int height) = GetImageDimensions(IFormFile);

                string sql = @"INSERT INTO picture(fid, zip_type, zip_value, width, height, picture_path, picture_arg, created)
                                VALUES(@fid, @zip_type, @zip_value, @width, @height, @picture_path, @picture_arg, @created);";
                using (var conn = _context.CreateConnection())
                {
                    await conn.QueryAsync(sql, new
                    {
                        fid = fid,
                        zip_type = "w",
                        zip_value = 1080,
                        width = width,
                        height = height,
                        picture_path = fileNamePath,
                        picture_arg = "[]",
                        created = unixTimestamp
                    });
                    return fid;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return 0;
            }
        }

        private static (int, int) GetImageDimensions(IFormFile imageFile)
        {
            using (var image = Image.FromStream(imageFile.OpenReadStream()))
            {
                int width = image.Width;
                int height = image.Height;
                return (width, height);
            }
        }

        private IFormFile Base64ToImage(JObject jObject)
        {
            var unixTimestamp = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            IFormFile file = null;
            try
            {
                string type = (string)jObject["type"];
                string thumbUrl = (string)jObject["thumbUrl"];
                //"data:image/png;base64,iVBORw0KGgoAAAANSU"
                string[] arrayThumbUrl = thumbUrl.Split(',');
                string[] arrayType = type.Split('/');

                string newFileName = unixTimestamp.ToString() + "." + arrayType[1];

                byte[] bytes = Convert.FromBase64String(arrayThumbUrl[1]);
                MemoryStream stream = new MemoryStream(bytes); /*(string)jObject["name"]*/
                file = new FormFile(stream, 0, bytes.Length, "test", newFileName);
                return file;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return file;
            }
        }


    }
}
