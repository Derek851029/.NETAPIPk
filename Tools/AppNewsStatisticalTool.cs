using Dapper;
using PKApp.ConfigOptions;
using PKApp.DIObject;

namespace PKApp.Tools
{
    public class AppNewsStatisticalTool
    {
        private readonly DapperContext _content;
        public AppNewsStatisticalTool(DapperContext context)
        {
            _content = context;
        }

        public async Task<StatisicalNewsClass> AppNewsTotalData(int start, int end, string brand)
        {
            string sql = "";
            using (var conn = _content.CreateConnection())
            {
                sql = @"SELECT COUNT(*) AS total FROM member a
                        LEFT JOIN member_accesstoken b on a.mid = b.mid
                        LEFT JOIN api_register_device c on b.devicecode_id = c.devicecode_id
                        where devicetoken != ''";
                var memberData = await conn.QueryAsync(sql);

                int totalMember = int.Parse(memberData.FirstOrDefault().total.ToString());

                sql = @"SELECT a.*,b.brand FROM report_app_news_read a
                        LEFT JOIN app_news b on a.app_news_id = b.app_news_id
                        WHERE send_last_time > @start and send_last_time < @end ";
                sql += brand == "" ? "" : "AND b.brand = @brand";
                var repotAppNewsData = await conn.QueryAsync(sql, new
                {
                    start = start,
                    end = end,
                    brand = brand,
                });
                int totalMessage = 0;
                int totalSend = 0;
                var firstId = repotAppNewsData.FirstOrDefault().app_news_id;
                List<int> ids = new List<int>();

                foreach (var item in repotAppNewsData)
                {
                    totalMessage += 1;
                    totalSend += Convert.ToInt32(item.send_count);
                    ids.Add(Convert.ToInt32(item.app_news_id));
                }

                sql = @"SELECT COUNT(*) as total FROM logs_app_news_read 
                        WHERE app_news_id >= @firstId ";
                sql += brand == "" ? "" : "AND origin = @brand";
                var readData = await conn.QueryAsync(sql, new
                {
                    firstId = firstId,
                    brand = brand
                });
                int totalRead = Convert.ToInt32(readData.FirstOrDefault().total);
                float percent = (float)totalRead / totalSend;
                float roundedPercent = (float)Math.Round(percent * 100, 3);

                sql = @"SELECT b.app_news_id, b.title, b.brand, 
                        COUNT(logs.app_news_id) as readCount,
                        c.send_count
                        FROM app_news b
                        LEFT JOIN logs_app_news_read logs ON logs.app_news_id = b.app_news_id
                        LEFT JOIN report_app_news_read c ON c.app_news_id = b.app_news_id
                        WHERE b.app_news_id >= @firstId  AND c.send_count IS NOT NULL 
                        GROUP BY b.app_news_id, b.title, b.brand, c.send_count ";
                sql += brand == "" ? "" : "AND b.brand = @brand";

                var sigleNews = await conn.QueryAsync(sql, new
                {
                    firstId = firstId,
                    brand = brand,
                });
                sql = $"SELECT * FROM pkcard_rlcms.logs_app_event where primary_id in ({string.Join(",", ids)})";
                var clickNews = await conn.QueryAsync(sql);
                var listClickNews = clickNews.ToList();

                List<dynamic> list = new List<dynamic> { };
                foreach (var item in sigleNews)
                {
                    int clickCount = listClickNews.Count(x => x.primary_id == item.app_news_id);
                    int readCount = Convert.ToInt32(item.readCount);
                    int send_count = Convert.ToInt32(item.send_count);
                    float singlePercent = (float)readCount / send_count;
                    float singleRoundedPercent = (float)Math.Round(singlePercent * 100, 3);
                    var data = new
                    {
                        ID = Convert.ToInt32(item.app_news_id),
                        Title = item.title,
                        Member = totalMember,
                        Send = Convert.ToInt32(item.send_count),
                        Open = Convert.ToInt32(item.readCount),
                        click = clickCount,
                        Percent = singleRoundedPercent,
                    };

                    list.Add(data);
                }
                StatisicalNewsClass statisicalNews = new StatisicalNewsClass();
                statisicalNews.TotalMessage = totalMessage;
                statisicalNews.TotalMember = totalMember;
                statisicalNews.TotalSend = totalSend;
                statisicalNews.TotalRead = totalRead;
                statisicalNews.Percent = roundedPercent;
                statisicalNews.SingleNewsData = list;

                return statisicalNews;
            }

        }
    }
}
