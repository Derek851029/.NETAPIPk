using Dapper;
using PKApp.DIObject;
using PKApp.Services;

namespace PKApp.Tools
{
    public class VoucherSettingTool
    {
        private readonly IConfiguration _configuration;
        private readonly DapperContext _context;
        private readonly IFilesService _filesService;

        long today = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();

        public VoucherSettingTool(
            IConfiguration configuration,
            DapperContext context,
            IFilesService filesService
            )
        {
            _configuration = configuration;
            _context = context;
            _filesService = filesService;
        }

        public async Task<int> Insert(VoucherData voucherData, string aid)
        {
            int voucherId = 0;
            string sql = "";
            int image_fid = 0;
            using (var conn = _context.CreateConnection())
            {
                if (voucherData.Activity_image != null) { image_fid = await _filesService.UploadFilesToGCS(voucherData, "image", "app_voucher"); }

                sql = @"INSERT INTO app_voucher(category_id,category_name,group_name,title,sub_title,group_id,
                        groupDesc,image_url,start_time,end_time,status,deleted,created,created_aid,changed,changed_aid)
                        VALUES(@category_id,@category_name,@group_name,@title,@sub_title,@group_id,
                        @groupDesc,@image_url,@start_time,@end_time,@status,@deleted,@created,@created_aid,@changed,@changed_aid);

                        SELECT CAST(LAST_INSERT_ID() AS UNSIGNED INTEGER)";

                await conn.QueryAsync(sql, new
                {
                    category_id = voucherData.Category_id,
                    category_name = voucherData.Category_name,
                    group_name = voucherData.Group_name,
                    title = voucherData.Title,
                    sub_title = voucherData.Sub_title ?? "",
                    group_id = voucherData.Group_id,
                    groupDesc = voucherData.GroupDesc,
                    start_time = DateTimeOffset.Parse(voucherData.Time[0]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                    end_time = DateTimeOffset.Parse(voucherData.Time[1]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                    status = voucherData.Status,
                    deleted = 0,
                    image_url = image_fid == 0 ? voucherData.Image_url : image_fid.ToString(),
                    changed = 0,
                    changed_aid = 0,
                    created = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                    created_aid = aid,
                });
            }
            return voucherId;
        }

        public async Task<int> Update(VoucherData voucherData, int voucherId, string aid)
        {
            string sql = "";
            int image_fid = 0;
            using (var conn = _context.CreateConnection())
            {
                if (voucherData.Activity_image != null) { image_fid = await _filesService.UploadFilesToGCS(voucherData, "image", "app_news"); }

                if (image_fid == 0)
                {
                    sql = @"select image_url from app_voucher where app_voucher_id = @app_voucher_id";
                    string dataImage = await conn.QuerySingleOrDefaultAsync<string>(sql, new { app_voucher_id = voucherId });
                    if (int.TryParse(dataImage, out int fid))
                    {
                        if (string.IsNullOrEmpty(voucherData.Image_url))
                        {
                            image_fid = fid;
                        }

                    }
                }

                sql = @"UPDATE app_voucher SET category_id = @category_id, category_name = @category_name, group_name = @group_name, title = @title, sub_title = @sub_title,
                                    group_id = @group_id, groupDesc = @groupDesc, image_url = @image_url,
                                    start_time = @start_time, end_time = @end_time, status = @status, changed = @changed, changed_aid = @changed_aid
                                    WHERE app_voucher_id = @app_voucher_id;";

                await conn.QueryAsync(sql, new
                {
                    category_id = voucherData.Category_id,
                    category_name = voucherData.Category_name,
                    group_name = voucherData.Group_name,
                    title = voucherData.Title,
                    sub_title = voucherData.Sub_title ?? "",
                    group_id = voucherData.Group_id,
                    groupDesc = voucherData.GroupDesc,
                    image_url = image_fid == 0 ? voucherData.Image_url : image_fid.ToString(),
                    start_time = DateTimeOffset.Parse(voucherData.Time[0]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                    end_time = DateTimeOffset.Parse(voucherData.Time[1]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                    status = voucherData.Status,
                    changed = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                    changed_aid = aid,
                    app_voucher_id = voucherId,
                });
            }
            return voucherId;
        }
    }
}
