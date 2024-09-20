using Dapper;
using PKApp.DIObject;

namespace PKApp.Tools
{
    public class DynamicTool
    {
        private readonly DapperContext _context;
        public DynamicTool(DapperContext context)
        {
            _context = context;
        }

        public async Task<dynamic> GetDynamic(string linkType)
        {
            string sql = @"SELECT * FROM dynamic_link WHERE link_type = @link_type order by created desc";
            using (var conn = _context.CreateConnection())
            {
                var dynamicData = conn.QueryAsync(sql, new
                {
                    link_type = linkType
                });

                return dynamicData;
            }
        }
    }
}
