using Dapper;
using Newtonsoft.Json.Linq;
using PKApp.DIObject;
using PKApp.Services;

namespace PKApp.Tools
{
    public class WebActivityTool
    {
        private readonly IConfiguration _configuration;
        private readonly DapperContext _context;
        private readonly IFilesService _filesService;

        long today = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();

        public WebActivityTool(
            IConfiguration configuration,
            DapperContext context,
            IFilesService filesService
            )
        {
            _configuration = configuration;
            _context = context;
            _filesService = filesService;
        }

        public async Task<int> InsertDrawPrize(JObject webActivityData)
        {
            int activity_id = 0;
            string sql = "";

            string start = (string)webActivityData["time"][0];
            string end = (string)webActivityData["time"][1];

            long start_time = DateTimeOffset.Parse(start).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            long end_time = DateTimeOffset.Parse(end).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();

            using (var conn = _context.CreateConnection2())
            {
                activity_id = await InsertActivity(webActivityData);

                sql = @"INSERT INTO activity_drawprize
                        (activity_id,background_color,font_color,content_background_color,content_font_color,game_start_path,game_background_path,
                        game_title_path,game_content_path,draw_title,draw_path,draw_content,draw_note,win_path,no_win_path,cd_path,modify)VALUES
                        (@activity_id,@background_color,@font_color,@content_background_color,@content_font_color,@game_start_path,@game_background_path,
                        @game_title_path,@game_content_path,@draw_title,@draw_path,@draw_content,@draw_note,@win_path, @no_win_path,@cd_path,@modify);";
                await conn.QueryAsync(sql, new
                {
                    activity_id = activity_id,
                    background_color = (string)webActivityData["background_color"],
                    font_color = (string)webActivityData["font_color"],
                    content_background_color = (string)webActivityData["content_background_color"],
                    content_font_color = (string)webActivityData["content_font_color"],
                    game_start_path = await _filesService.UploadFilesToGCS((string)webActivityData["start_image"], "webactivity", "webactivity"),
                    game_background_path = await _filesService.UploadFilesToGCS((string)webActivityData["background_image"], "webactivity", "webactivity"),
                    game_title_path = await _filesService.UploadFilesToGCS((string)webActivityData["title_image"], "webactivity", "webactivity"),
                    game_content_path = await _filesService.UploadFilesToGCS((string)webActivityData["content_image"], "webactivity", "webactivity"),
                    draw_title = (string)webActivityData["draw_title"],
                    draw_path = await _filesService.UploadFilesToGCS((string)webActivityData["draw_image"], "webactivity", "webactivity"),
                    draw_content = (string)webActivityData["draw_content"],
                    draw_note = (string)webActivityData["draw_note"],
                    win_path = await _filesService.UploadFilesToGCS((string)webActivityData["win_image"], "webactivity", "webactivity"),
                    no_win_path = await _filesService.UploadFilesToGCS((string)webActivityData["no_win_image"], "webactivity", "webactivity"),
                    cd_path = await _filesService.UploadFilesToGCS((string)webActivityData["cd_image"], "webactivity", "webactivity"),
                    modify = today,
                });

                for (var i = 0; i < 100; i++)
                {
                    string prize_name = $"prize_name{i}";
                    string coupon_id = $"coupon_id{i}";
                    string prize_amount = $"prize_amount{i}";
                    string brand = $"brand{i}";
                    string prize_image = $"prize_image{i}";
                    if (webActivityData.ContainsKey(prize_name))
                    {
                        sql = @"INSERT INTO activity_drawprize_prize
                                    (activity_id,prize_name,coupon_id,chance,prize_amount,start_time,end_time,
                                    brand,prize_path,prize_take_amount,created,modify)VALUES
                                    (@activity_id,@prize_name,@coupon_id,@chance,@prize_amount,@start_time,@end_time,
                                    @brand,@prize_path, @prize_take_amount,@created,@modify);";
                        await conn.QueryAsync(sql, new
                        {
                            activity_id = activity_id,
                            prize_name = (string)webActivityData[prize_name],
                            coupon_id = (string)webActivityData[coupon_id],
                            chance = 0,
                            prize_amount = (string)webActivityData[prize_amount],
                            start_time = start_time,
                            end_time = end_time,
                            brand = (string)webActivityData[brand],
                            prize_path = await _filesService.UploadFilesToGCS((string)webActivityData[prize_image], "webactivity", "webactivity"),
                            prize_take_amount = 0,
                            created = today,
                            modify = 0,
                        });
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return activity_id;
        }

        public async Task<int> InsertSlots(JObject webActivityData)
        {
            int activity_id = 0;
            string sql = "";

            string start = (string)webActivityData["time"][0];
            string end = (string)webActivityData["time"][1];

            long start_time = DateTimeOffset.Parse(start).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            long end_time = DateTimeOffset.Parse(end).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();

            using (var conn = _context.CreateConnection2())
            {

                activity_id = await InsertActivity(webActivityData);

                sql = @"INSERT INTO activity_slots
                    (activity_id,background_color,font_color,content_background_color,content_font_color,footer_path,slots_footer_path,
                    slots_frame_path,slots_drawbar_path,slots_drawbar_ball_path,tip_path,slots_title,slots_path,slots_content, slots_note, 
                    win_path,no_win_path,cd_path,modify)VALUES
                    (@activity_id,@background_color,@font_color,@content_background_color,@content_font_color,@footer_path,@slots_footer_path,
                    @slots_frame_path,@slots_drawbar_path,@slots_drawbar_ball_path,@tip_path,@slots_title,@slots_path,@slots_content,slots_note, 
                    @win_path, @no_win_path,@cd_path,@modify);";
                await conn.QueryAsync(sql, new
                {
                    activity_id = activity_id,
                    background_color = (string)webActivityData["background_color"],
                    font_color = (string)webActivityData["font_color"],
                    content_background_color = (string)webActivityData["content_background_color"],
                    content_font_color = (string)webActivityData["content_font_color"],
                    footer_path = await _filesService.UploadFilesToGCS((string)webActivityData["footer_image"], "webactivity", "webactivity"),
                    slots_footer_path = await _filesService.UploadFilesToGCS((string)webActivityData["slotsfooter_image"], "webactivity", "webactivity"),
                    slots_frame_path = await _filesService.UploadFilesToGCS((string)webActivityData["slotsframe_image"], "webactivity", "webactivity"),
                    slots_drawbar_path = await _filesService.UploadFilesToGCS((string)webActivityData["drawbar_image"], "webactivity", "webactivity"),
                    slots_drawbar_ball_path = await _filesService.UploadFilesToGCS((string)webActivityData["slotsDrawbarBall_image"], "webactivity", "webactivity"),
                    tip_path = await _filesService.UploadFilesToGCS((string)webActivityData["tip_image"], "webactivity", "webactivity"),
                    slots_title = (string)webActivityData["draw_title"],
                    slots_path = await _filesService.UploadFilesToGCS((string)webActivityData["draw_image"], "webactivity", "webactivity"),
                    slots_content = (string)webActivityData["draw_content"],
                    slots_note = (string)webActivityData["draw_note"],
                    win_path = await _filesService.UploadFilesToGCS((string)webActivityData["win_image"], "webactivity", "webactivity"),
                    no_win_path = await _filesService.UploadFilesToGCS((string)webActivityData["no_win_image"], "webactivity", "webactivity"),
                    cd_path = await _filesService.UploadFilesToGCS((string)webActivityData["cd_image"], "webactivity", "webactivity"),
                    modify = today,
                });

                string item_image = await _filesService.UploadFilesToGCS((string)webActivityData["item_image"], "webactivity", "item_image", activity_id);
                for (var i = 0; i < 100; i++)
                {
                    string prize_name = $"prize_name{i}";
                    string coupon_id = $"coupon_id{i}";
                    string prize_amount = $"prize_amount{i}";
                    string brand = $"brand{i}";
                    string prize_image = $"prize_image{i}";
                    //string item_image = $"item_image{i}";
                    if (webActivityData.ContainsKey(prize_name))
                    {
                        sql = @"INSERT INTO activity_slots_prize
                                    (activity_id,prize_name,coupon_id,chance,prize_amount,start_time,end_time,
                                    brand,item_path, prize_path,prize_take_amount,created,modify)VALUES
                                    (@activity_id,@prize_name,@coupon_id,@chance,@prize_amount,@start_time,@end_time,
                                    @brand,@item_path, @prize_path, @prize_take_amount,@created,@modify);";
                        await conn.QueryAsync(sql, new
                        {
                            activity_id = activity_id,
                            prize_name = (string)webActivityData[prize_name],
                            coupon_id = (string)webActivityData[coupon_id],
                            chance = 0,
                            prize_amount = (string)webActivityData[prize_amount],
                            start_time = start_time,
                            end_time = end_time,
                            brand = (string)webActivityData[brand],
                            item_path = item_image,
                            prize_path = await _filesService.UploadFilesToGCS((string)webActivityData[prize_image], "webactivity", "webactivity"),
                            prize_take_amount = 0,
                            created = today,
                            modify = 0,
                        });
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return activity_id;
        }

        public async Task<int> InsertQuiz(JObject webActivityData)
        {
            int activity_id = 0;
            string sql = "";

            string start = (string)webActivityData["time"][0];
            string end = (string)webActivityData["time"][1];

            long start_time = DateTimeOffset.Parse(start).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            long end_time = DateTimeOffset.Parse(end).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();

            using (var conn = _context.CreateConnection2())
            {
                activity_id = await InsertActivity(webActivityData);

                sql = @"INSERT INTO activity_quiz
                        (activity_id,background_color,font_color,content_background_color,content_font_color,question_font_color,
                        game_start_path,game_background_path,
                        game_title_path,quiz_title,quiz_path,quiz_content,quiz_note,win_path,no_win_path,cd_path,modify)VALUES
                        (@activity_id,@background_color,@font_color,@content_background_color,@content_font_color,@question_font_color,
                        @game_start_path,@game_background_path,
                        @game_title_path,@quiz_title,@quiz_path,@quiz_content,@quiz_note,@win_path, @no_win_path,@cd_path,@modify);";
                await conn.QueryAsync(sql, new
                {
                    activity_id = activity_id,
                    background_color = (string)webActivityData["background_color"],
                    font_color = (string)webActivityData["font_color"],
                    content_background_color = (string)webActivityData["content_background_color"],
                    content_font_color = (string)webActivityData["content_font_color"],
                    question_font_color = (string)webActivityData["question_font_color"],
                    game_start_path = await _filesService.UploadFilesToGCS((string)webActivityData["start_image"], "webactivity", "webactivity"),
                    game_background_path = await _filesService.UploadFilesToGCS((string)webActivityData["background_image"], "webactivity", "webactivity"),
                    game_title_path = await _filesService.UploadFilesToGCS((string)webActivityData["title_image"], "webactivity", "webactivity"),
                    quiz_title = (string)webActivityData["draw_title"],
                    quiz_path = await _filesService.UploadFilesToGCS((string)webActivityData["draw_image"], "webactivity", "webactivity"),
                    quiz_content = (string)webActivityData["draw_content"],
                    quiz_note = (string)webActivityData["draw_note"],
                    win_path = await _filesService.UploadFilesToGCS((string)webActivityData["win_image"], "webactivity", "webactivity"),
                    no_win_path = await _filesService.UploadFilesToGCS((string)webActivityData["no_win_image"], "webactivity", "webactivity"),
                    cd_path = await _filesService.UploadFilesToGCS((string)webActivityData["cd_image"], "webactivity", "webactivity"),
                    modify = today,
                });

                for (var i = 0; i < 100; i++)
                {
                    string subject = $"subject{i}";
                    string option_a = $"option_a{i}";
                    string option_a_image = $"option_a_image{i}";
                    string option_b = $"option_b{i}";
                    string option_b_image = $"option_b_image{i}";
                    if (webActivityData.ContainsKey(subject))
                    {
                        sql = @"INSERT INTO activity_quiz_question
                                    (activity_id,subject,option_a,reply_a_path,option_b,reply_b_path,created,
                                    modify)VALUES
                                    (@activity_id,@subject,@option_a,@reply_a_path,@option_b,@reply_b_path,@created,
                                    @modify);";
                        await conn.QueryAsync(sql, new
                        {
                            activity_id = activity_id,
                            subject = (string)webActivityData[subject],
                            option_a = (string)webActivityData[option_a],
                            reply_a_path = await _filesService.UploadFilesToGCS((string)webActivityData[option_a_image], "webactivity", "webactivity"),
                            option_b = (string)webActivityData[option_b],
                            reply_b_path = await _filesService.UploadFilesToGCS((string)webActivityData[option_b_image], "webactivity", "webactivity"),
                            created = today,
                            modify = 0,
                        });
                    }
                    else
                    {
                        break;
                    }
                }

                for (var i = 0; i < 100; i++)
                {
                    string prize_name = $"prize_name{i}";
                    string coupon_id = $"coupon_id{i}";
                    string prize_amount = $"prize_amount{i}";
                    string brand = $"brand{i}";
                    string option = $"option{i}";
                    string prize_image = $"prize_image{i}";
                    if (webActivityData.ContainsKey(prize_name))
                    {
                        sql = @"INSERT INTO activity_quiz_prize
                                    (activity_id,prize_name,coupon_id,chance,prize_amount,start_time,end_time,
                                    brand,`option`,prize_path,prize_take_amount,created,modify)VALUES
                                    (@activity_id,@prize_name,@coupon_id,@chance,@prize_amount,@start_time,@end_time,
                                    @brand,@option,@prize_path, @prize_take_amount,@created,@modify);";
                        await conn.QueryAsync(sql, new
                        {
                            activity_id = activity_id,
                            prize_name = (string)webActivityData[prize_name],
                            coupon_id = (string)webActivityData[coupon_id],
                            chance = 0,
                            prize_amount = (string)webActivityData[prize_amount],
                            start_time = start_time,
                            end_time = end_time,
                            brand = (string)webActivityData[brand],
                            option = (string)webActivityData[option],
                            prize_path = await _filesService.UploadFilesToGCS((string)webActivityData[prize_image], "webactivity", "webactivity"),
                            prize_take_amount = 0,
                            created = today,
                            modify = 0,
                        });
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return activity_id;
        }

        public async Task<int> UpdateDrawPrize(JObject webActivityData, int webActivityID)
        {
            string sql = "";
            int icon_fid = 0;
            using (var conn = _context.CreateConnection2())
            {
                long sTime = DateTimeOffset.Parse((string)webActivityData["time"][0]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                long eTime = DateTimeOffset.Parse((string)webActivityData["time"][1]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();

                if (webActivityData["Activity_image"] != null) { icon_fid = await _filesService.UploadFilesToGCS(webActivityData, "image", "webactivity"); }
                else
                {
                    sql = @"SELECT icon_fid FROM activity WHERE activity_id = @activity_id";
                    icon_fid = await conn.QuerySingleAsync<int>(sql, new { activity_id = webActivityID });
                }

                sql = @"UPDATE activity
                        SET activity_type = @activity_type, title = @title, start_time = @start_time,end_time = @end_time,
                        chance = @chance,play_interval = @play_interval,play_target = @play_target,is_free = @is_free,
                        icon_fid = @icon_fid, modify = @modify WHERE activity_id = @activity_id";

                await conn.QueryAsync<int>(sql, new
                {
                    activity_type = (string)webActivityData["activity_type"],
                    title = (string)webActivityData["title"],
                    start_time = sTime,
                    end_time = eTime,
                    chance = (int)webActivityData["chance"],
                    play_interval = (int)webActivityData["play_interval"],
                    play_target = (string)webActivityData["play_target"],
                    is_free = (int)webActivityData["is_free"],
                    icon_fid = icon_fid,
                    modify = today,
                    activity_id = webActivityID,
                });

                sql = @"SELECT * FROM activity_drawprize WHERE activity_id = @activity_id";
                var activity_drawprize_data = await conn.QueryAsync(sql, new { activity_id = webActivityID });

                sql = @"UPDATE activity_drawprize
                                SET background_color = @background_color,font_color = @font_color,content_background_color = @content_background_color,
                                content_font_color = @content_font_color,game_start_path = @game_start_path,game_background_path = @game_background_path,
                                game_title_path = @game_title_path,game_content_path = @game_content_path,draw_title = @draw_title,
                                draw_path = @draw_path,draw_content = @draw_content,draw_note = @draw_note,win_path = @win_path,no_win_path = @no_win_path,
                                cd_path = @cd_path,modify = @modify WHERE activity_id = @activity_id";

                await conn.QueryAsync(sql, new
                {
                    activity_id = webActivityID,
                    background_color = (string)webActivityData["background_color"],
                    font_color = (string)webActivityData["font_color"],
                    content_background_color = (string)webActivityData["content_background_color"],
                    content_font_color = (string)webActivityData["content_font_color"],
                    modify = today,
                    draw_title = (string)webActivityData["draw_title"],
                    draw_content = (string)webActivityData["draw_content"],
                    draw_note = (string)webActivityData["draw_note"],

                    game_start_path = (string)webActivityData["start_image"] == null ? activity_drawprize_data.First().game_start_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["start_image"], "webactivity", "webactivity"),

                    game_background_path = (string)webActivityData["background_image"] == null ? activity_drawprize_data.First().game_background_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["background_image"], "webactivity", "webactivity"),

                    game_title_path = (string)webActivityData["title_image"] == null ? activity_drawprize_data.First().game_title_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["title_image"], "webactivity", "webactivity"),

                    game_content_path = (string)webActivityData["content_image"] == null ? activity_drawprize_data.First().game_content_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["content_image"], "webactivity", "webactivity"),

                    draw_path = (string)webActivityData["draw_image"] == null ? activity_drawprize_data.First().draw_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["draw_image"], "webactivity", "webactivity"),

                    win_path = (string)webActivityData["win_image"] == null ? activity_drawprize_data.First().win_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["win_image"], "webactivity", "webactivity"),

                    no_win_path = (string)webActivityData["no_win_image"] == null ? activity_drawprize_data.First().no_win_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["no_win_image"], "webactivity", "webactivity"),

                    cd_path = (string)webActivityData["cd_image"] == null ? activity_drawprize_data.First().cd_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["cd_image"], "webactivity", "webactivity"),
                });


                for (var i = 0; i < 100; i++)
                {
                    string prize_name = $"prize_name{i}";
                    string coupon_id = $"coupon_id{i}";
                    string prize_amount = $"prize_amount{i}";
                    string brand = $"brand{i}";
                    string prize_image = $"prize_image{i}";
                    string prize_id = $"prize_id{i}";

                    if (webActivityData.ContainsKey(prize_name))
                    {
                        sql = @"SELECT * FROM activity_drawprize_prize WHERE prize_id = @prize_id";
                        var prizeData = await conn.QueryAsync(sql, new { prize_id = (string)webActivityData[prize_id] });

                        if (prizeData.Any())
                        {
                            sql = @"UPDATE activity_drawprize_prize
                                            SET prize_name = @prize_name,coupon_id = @coupon_id,chance = @chance,prize_amount = @prize_amount,
                                            start_time = @start_time, end_time = @end_time, brand = @brand,prize_path = @prize_path,modify = @modify WHERE prize_id = @prize_id;";
                            await conn.QueryAsync(sql, new
                            {
                                prize_id = (string)webActivityData[prize_id],
                                prize_name = (string)webActivityData[prize_name],
                                coupon_id = (string)webActivityData[coupon_id],
                                chance = 0,
                                prize_amount = (string)webActivityData[prize_amount],
                                start_time = sTime,
                                end_time = eTime,
                                brand = (string)webActivityData[brand],

                                prize_path = (string)webActivityData[prize_image] == null ? prizeData.First().prize_path :
                                await _filesService.UploadFilesToGCS((string)webActivityData[prize_image], "webactivity", "webactivity"),

                                modify = today,
                            });
                        }
                        else
                        {
                            sql = @"INSERT INTO activity_drawprize_prize
                                            (activity_id,prize_name,coupon_id,chance,prize_amount,start_time,end_time,
                                            brand,prize_path,prize_take_amount,created,modify)VALUES
                                            (@activity_id,@prize_name,@coupon_id,@chance,@prize_amount,@start_time,@end_time,
                                            @brand,@prize_path, @prize_take_amount,@created,@modify);";
                            await conn.QueryAsync(sql, new
                            {
                                activity_id = webActivityID,
                                prize_name = (string)webActivityData[prize_name],
                                coupon_id = (string)webActivityData[coupon_id],
                                chance = 0,
                                prize_amount = (string)webActivityData[prize_amount],
                                start_time = sTime,
                                end_time = eTime,
                                brand = (string)webActivityData[brand],
                                prize_path = await _filesService.UploadFilesToGCS((string)webActivityData[prize_image], "webactivity", "webactivity"),
                                prize_take_amount = 0,
                                created = today,
                                modify = 0,
                            });
                        }

                    }
                    else
                    {
                        sql = @"SELECT * FROM activity_drawprize_prize WHERE prize_id = @prize_id";
                        var findData = await conn.QueryAsync(sql, new { prize_id = (string)webActivityData[prize_id] });
                        if (findData.Any())
                        {
                            sql = @"DELETE FROM activity_drawprize_prize WHERE prize_id = @prize_id";
                            await conn.QueryAsync(sql, new { prize_id = (string)webActivityData[prize_id] });
                        }
                        else
                        {
                            break;
                        }

                    }
                }
            }

            return webActivityID;
        }

        public async Task<int> UpdateSlots(JObject webActivityData, int webActivityID)
        {
            string sql = "";
            int icon_fid = 0;
            using (var conn = _context.CreateConnection2())
            {
                long sTime = DateTimeOffset.Parse((string)webActivityData["time"][0]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                long eTime = DateTimeOffset.Parse((string)webActivityData["time"][1]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();

                if (webActivityData["Activity_image"] != null) { icon_fid = await _filesService.UploadFilesToGCS(webActivityData, "image", "webactivity"); }
                else
                {
                    sql = @"SELECT icon_fid FROM activity WHERE activity_id = @activity_id";
                    icon_fid = await conn.QuerySingleAsync<int>(sql, new { activity_id = webActivityID });
                }

                sql = @"UPDATE activity
                        SET activity_type = @activity_type, title = @title, start_time = @start_time,end_time = @end_time,
                        chance = @chance,play_interval = @play_interval,play_target = @play_target,is_free = @is_free,
                        icon_fid = @icon_fid, modify = @modify WHERE activity_id = @activity_id";

                await conn.QueryAsync<int>(sql, new
                {
                    activity_type = (string)webActivityData["activity_type"],
                    title = (string)webActivityData["title"],
                    start_time = sTime,
                    end_time = eTime,
                    chance = (int)webActivityData["chance"],
                    play_interval = (int)webActivityData["play_interval"],
                    play_target = (string)webActivityData["play_target"],
                    is_free = (int)webActivityData["is_free"],
                    icon_fid = icon_fid,
                    modify = today,
                    activity_id = webActivityID,
                });

                sql = @"SELECT * FROM activity_slots WHERE activity_id = @activity_id";
                var activity_slots_data = await conn.QueryAsync(sql, new { activity_id = webActivityID });

                sql = @"UPDATE activity_slots
                        SET background_color = @background_color,font_color = @font_color,content_background_color = @content_background_color,
                        content_font_color = @content_font_color,footer_path = @footer_path,slots_footer_path = @slots_footer_path,
                        slots_frame_path = @slots_frame_path,slots_drawbar_path = @slots_drawbar_path,slots_drawbar_ball_path = @slots_drawbar_ball_path,
                        tip_path = @tip_path, slots_title = @slots_title,
                        slots_path = @slots_path,slots_content = @slots_content,slots_note = @slots_note,win_path = @win_path,no_win_path = @no_win_path,
                        cd_path = @cd_path,modify = @modify WHERE activity_id = @activity_id";

                await conn.QueryAsync(sql, new
                {
                    activity_id = webActivityID,
                    background_color = (string)webActivityData["background_color"],
                    font_color = (string)webActivityData["font_color"],
                    content_background_color = (string)webActivityData["content_background_color"],
                    content_font_color = (string)webActivityData["content_font_color"],
                    modify = today,
                    slots_title = (string)webActivityData["draw_title"],
                    slots_content = (string)webActivityData["draw_content"],
                    slots_note = (string)webActivityData["draw_note"],

                    footer_path = (string)webActivityData["footer_image"] == null ? activity_slots_data.First().footer_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["footer_image"], "webactivity", "webactivity"),

                    slots_footer_path = (string)webActivityData["slotsfooter_image"] == null ? activity_slots_data.First().slots_footer_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["slotsfooter_image"], "webactivity", "webactivity"),

                    slots_frame_path = (string)webActivityData["slotsframe_image"] == null ? activity_slots_data.First().slots_frame_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["slotsframe_image"], "webactivity", "webactivity"),

                    slots_drawbar_path = (string)webActivityData["drawbar_image"] == null ? activity_slots_data.First().slots_drawbar_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["drawbar_image"], "webactivity", "webactivity"),

                    slots_drawbar_ball_path = (string)webActivityData["slotsDrawbarBall_image"] == null ? activity_slots_data.First().slots_drawbar_ball_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["slotsDrawbarBall_image"], "webactivity", "webactivity"),

                    tip_path = (string)webActivityData["tip_image"] == null ? activity_slots_data.First().tip_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["tip_image"], "webactivity", "webactivity"),

                    slots_path = (string)webActivityData["draw_image"] == null ? activity_slots_data.First().slots_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["draw_image"], "webactivity", "webactivity"),

                    win_path = (string)webActivityData["win_image"] == null ? activity_slots_data.First().win_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["win_image"], "webactivity", "webactivity"),

                    no_win_path = (string)webActivityData["no_win_image"] == null ? activity_slots_data.First().no_win_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["no_win_image"], "webactivity", "webactivity"),

                    cd_path = (string)webActivityData["cd_image"] == null ? activity_slots_data.First().cd_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["cd_image"], "webactivity", "webactivity"),
                });

                string item_image = (string)webActivityData["item_image"] == null ? "" :
                        await _filesService.UploadFilesToGCS((string)webActivityData["item_image"], "webactivity", "item_image", webActivityID);

                for (var i = 0; i < 100; i++)
                {
                    string prize_name = $"prize_name{i}";
                    string coupon_id = $"coupon_id{i}";
                    string prize_amount = $"prize_amount{i}";
                    string brand = $"brand{i}";
                    string prize_image = $"prize_image{i}";
                    //string item_image = $"item_image{i}";
                    string prize_id = $"prize_id{i}";

                    if (webActivityData.ContainsKey(prize_name))
                    {
                        sql = @"SELECT * FROM activity_slots_prize WHERE prize_id = @prize_id";
                        var prizeData = await conn.QueryAsync(sql, new { prize_id = (string)webActivityData[prize_id] });

                        if (prizeData.Any())
                        {

                            sql = @"UPDATE activity_slots_prize
                                SET prize_name = @prize_name,coupon_id = @coupon_id,chance = @chance,prize_amount = @prize_amount,
                                start_time = @start_time, end_time = @end_time, brand = @brand, item_path = @item_path,
                                prize_path = @prize_path,modify = @modify WHERE prize_id = @prize_id;";
                            await conn.QueryAsync(sql, new
                            {
                                prize_id = (string)webActivityData[prize_id],
                                prize_name = (string)webActivityData[prize_name],
                                coupon_id = (string)webActivityData[coupon_id],
                                chance = 0,
                                prize_amount = (string)webActivityData[prize_amount],
                                start_time = sTime,
                                end_time = eTime,
                                brand = (string)webActivityData[brand],

                                prize_path = (string)webActivityData[prize_image] == null ? prizeData.First().prize_path :
                                await _filesService.UploadFilesToGCS((string)webActivityData[prize_image], "webactivity", "webactivity"),

                                item_path = item_image == "" ? prizeData.First().item_path : item_image,
                                modify = today,
                            });
                        }
                        else
                        {
                            sql = @"INSERT INTO activity_slots_prize
                                    (activity_id,prize_name,coupon_id,chance,prize_amount,start_time,end_time,
                                    brand,item_path, prize_path,prize_take_amount,created,modify)VALUES
                                    (@activity_id,@prize_name,@coupon_id,@chance,@prize_amount,@start_time,@end_time,
                                    @brand,@item_path, @prize_path, @prize_take_amount,@created,@modify);";
                            await conn.QueryAsync(sql, new
                            {
                                activity_id = webActivityID,
                                prize_name = (string)webActivityData[prize_name],
                                coupon_id = (string)webActivityData[coupon_id],
                                chance = 0,
                                prize_amount = (string)webActivityData[prize_amount],
                                start_time = sTime,
                                end_time = eTime,
                                brand = (string)webActivityData[brand],
                                item_path = item_image,
                                prize_path = await _filesService.UploadFilesToGCS((string)webActivityData[prize_image], "webactivity", "webactivity"),
                                prize_take_amount = 0,
                                created = today,
                                modify = 0,
                            });
                        }

                    }
                    else
                    {
                        sql = @"SELECT * FROM activity_slots_prize WHERE prize_id = @prize_id";
                        var findData = await conn.QueryAsync(sql, new { prize_id = (string)webActivityData[prize_id] });
                        if (findData.Any())
                        {
                            sql = @"DELETE FROM activity_slots_prize WHERE prize_id = @prize_id";
                            await conn.QueryAsync(sql, new { prize_id = (string)webActivityData[prize_id] });
                        }
                        else
                        {
                            break;
                        }

                    }
                }
            }

            return webActivityID;
        }

        public async Task<int> UpdateQuiz(JObject webActivityData, int webActivityID)
        {
            string sql = "";
            int icon_fid = 0;
            using (var conn = _context.CreateConnection2())
            {
                long sTime = DateTimeOffset.Parse((string)webActivityData["time"][0]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                long eTime = DateTimeOffset.Parse((string)webActivityData["time"][1]).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();

                if (webActivityData["Activity_image"] != null) { icon_fid = await _filesService.UploadFilesToGCS(webActivityData, "image", "webactivity"); }
                else
                {
                    sql = @"SELECT icon_fid FROM activity WHERE activity_id = @activity_id";
                    icon_fid = await conn.QuerySingleAsync<int>(sql, new { activity_id = webActivityID });
                }

                sql = @"UPDATE activity
                        SET activity_type = @activity_type, title = @title, start_time = @start_time,end_time = @end_time,
                        chance = @chance,play_interval = @play_interval,play_target = @play_target,is_free = @is_free,
                        icon_fid = @icon_fid, modify = @modify WHERE activity_id = @activity_id";

                await conn.QueryAsync<int>(sql, new
                {
                    activity_type = (string)webActivityData["activity_type"],
                    title = (string)webActivityData["title"],
                    start_time = sTime,
                    end_time = eTime,
                    chance = (int)webActivityData["chance"],
                    play_interval = (int)webActivityData["play_interval"],
                    play_target = (string)webActivityData["play_target"],
                    is_free = (int)webActivityData["is_free"],
                    icon_fid = icon_fid,
                    modify = today,
                    activity_id = webActivityID,
                });

                sql = @"SELECT * FROM activity_quiz WHERE activity_id = @activity_id";
                var activity_quiz_data = await conn.QueryAsync(sql, new { activity_id = webActivityID });

                sql = @"UPDATE activity_quiz
                        SET background_color = @background_color,font_color = @font_color,content_background_color = @content_background_color,
                        content_font_color = @content_font_color,question_font_color = @question_font_color,game_background_path = @game_background_path,
                        game_title_path = @game_title_path,quiz_title = @quiz_title,
                        quiz_path = @quiz_path,quiz_content = @quiz_content,quiz_note = @quiz_note,win_path = @win_path,no_win_path = @no_win_path,
                        cd_path = @cd_path,modify = @modify WHERE activity_id = @activity_id";

                await conn.QueryAsync(sql, new
                {
                    activity_id = webActivityID,
                    background_color = (string)webActivityData["background_color"],
                    font_color = (string)webActivityData["font_color"],
                    content_background_color = (string)webActivityData["content_background_color"],
                    content_font_color = (string)webActivityData["content_font_color"],
                    question_font_color = (string)webActivityData["question_font_color"],
                    modify = today,
                    quiz_title = (string)webActivityData["draw_title"],
                    quiz_content = (string)webActivityData["draw_content"],
                    quiz_note = (string)webActivityData["draw_note"],

                    game_background_path = (string)webActivityData["background_image"] == null ? activity_quiz_data.First().game_background_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["background_image"], "webactivity", "webactivity"),

                    game_title_path = (string)webActivityData["title_image"] == null ? activity_quiz_data.First().game_title_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["title_image"], "webactivity", "webactivity"),

                    quiz_path = (string)webActivityData["draw_image"] == null ? activity_quiz_data.First().quiz_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["draw_image"], "webactivity", "webactivity"),

                    win_path = (string)webActivityData["win_image"] == null ? activity_quiz_data.First().win_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["win_image"], "webactivity", "webactivity"),

                    no_win_path = (string)webActivityData["no_win_image"] == null ? activity_quiz_data.First().no_win_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["no_win_image"], "webactivity", "webactivity"),

                    cd_path = (string)webActivityData["cd_image"] == null ? activity_quiz_data.First().cd_path :
                    await _filesService.UploadFilesToGCS((string)webActivityData["cd_image"], "webactivity", "webactivity"),
                });

                for (var i = 0; i < 100; i++)
                {
                    string subject = $"subject{i}";
                    string option_a = $"option_a{i}";
                    string option_a_image = $"option_a_image{i}";
                    string option_b = $"option_b{i}";
                    string option_b_image = $"option_b_image{i}";
                    string question_id = $"question_id{i}";

                    if (webActivityData.ContainsKey(subject))
                    {
                        sql = @"SELECT * FROM activity_quiz_question WHERE question_id = @question_id";
                        var questionData = await conn.QueryAsync(sql, new { question_id = (string)webActivityData[question_id] });

                        if (questionData.Any())
                        {
                            sql = @"UPDATE activity_quiz_question SET subject = @subject, option_a = @option_a, reply_a_path = @reply_a_path,
                                    option_b = @option_b, reply_b_path = @reply_b_path, modify = @modify WHERE question_id = @question_id";
                            await conn.QueryAsync(sql, new
                            {
                                question_id = (string)webActivityData[question_id],
                                subject = (string)webActivityData[subject],
                                option_a = (string)webActivityData[option_a],
                                reply_a_path = (string)webActivityData[option_a_image] == null ? questionData.First().reply_a_path
                                : await _filesService.UploadFilesToGCS((string)webActivityData[option_a_image], "webactivity", "webactivity"),
                                option_b = (string)webActivityData[option_b],
                                reply_b_path = (string)webActivityData[option_b_image] == null ? questionData.First().reply_b_path
                                : await _filesService.UploadFilesToGCS((string)webActivityData[option_b_image], "webactivity", "webactivity"),
                                modify = today,
                            });
                        }
                        else
                        {
                            sql = @"INSERT INTO activity_quiz_question
                                    (activity_id,subject,option_a,reply_a_path,option_b,reply_b_path,created,
                                    modify)VALUES
                                    (@activity_id,@subject,@option_a,@reply_a_path,@option_b,@reply_b_path,@created,
                                    @modify);";
                            await conn.QueryAsync(sql, new
                            {
                                activity_id = webActivityID,
                                subject = (string)webActivityData[subject],
                                option_a = (string)webActivityData[option_a],
                                reply_a_path = await _filesService.UploadFilesToGCS((string)webActivityData[option_a_image], "webactivity", "webactivity"),
                                option_b = (string)webActivityData[option_b],
                                reply_b_path = await _filesService.UploadFilesToGCS((string)webActivityData[option_b_image], "webactivity", "webactivity"),
                                created = today,
                                modify = 0,
                            });
                        }

                    }
                    else
                    {
                        break;
                    }
                }

                for (var i = 0; i < 100; i++)
                {
                    string prize_name = $"prize_name{i}";
                    string coupon_id = $"coupon_id{i}";
                    string prize_amount = $"prize_amount{i}";
                    string brand = $"brand{i}";
                    string option = $"option{i}";
                    string prize_image = $"prize_image{i}";
                    string prize_id = $"prize_id{i}";

                    if (webActivityData.ContainsKey(prize_name))
                    {
                        sql = @"SELECT * FROM activity_drawprize_prize WHERE prize_id = @prize_id";
                        var prizeData = await conn.QueryAsync(sql, new { prize_id = (string)webActivityData[prize_id] });

                        if (prizeData.Any())
                        {
                            sql = @"UPDATE activity_quiz_prize
                                    SET prize_name = @prize_name,coupon_id = @coupon_id,chance = @chance,prize_amount = @prize_amount,
                                    start_time = @start_time, end_time = @end_time, brand = @brand,`option` = @option,
                                    prize_path = @prize_path,modify = @modify WHERE prize_id = @prize_id;";
                            await conn.QueryAsync(sql, new
                            {
                                prize_id = (string)webActivityData[prize_id],
                                prize_name = (string)webActivityData[prize_name],
                                coupon_id = (string)webActivityData[coupon_id],
                                chance = 0,
                                prize_amount = (string)webActivityData[prize_amount],
                                start_time = sTime,
                                end_time = eTime,
                                brand = (string)webActivityData[brand],
                                option = (string)webActivityData[option],
                                prize_path = (string)webActivityData[prize_image] == null ? prizeData.First().prize_path :
                                await _filesService.UploadFilesToGCS((string)webActivityData[prize_image], "webactivity", "webactivity"),

                                modify = today,
                            });
                        }
                        else
                        {
                            sql = @"INSERT INTO activity_quiz_prize
                                            (activity_id,prize_name,coupon_id,chance,prize_amount,start_time,end_time,
                                            brand,option,prize_path,prize_take_amount,created,modify)VALUES
                                            (@activity_id,@prize_name,@coupon_id,@chance,@prize_amount,@start_time,@end_time,
                                            @brand,@option,@prize_path, @prize_take_amount,@created,@modify);";
                            await conn.QueryAsync(sql, new
                            {
                                activity_id = webActivityID,
                                prize_name = (string)webActivityData[prize_name],
                                coupon_id = (string)webActivityData[coupon_id],
                                chance = 0,
                                prize_amount = (string)webActivityData[prize_amount],
                                start_time = sTime,
                                end_time = eTime,
                                brand = (string)webActivityData[brand],
                                option = (string)webActivityData[option],
                                prize_path = await _filesService.UploadFilesToGCS((string)webActivityData[prize_image], "webactivity", "webactivity"),
                                prize_take_amount = 0,
                                created = today,
                                modify = 0,
                            });
                        }

                    }
                    else
                    {
                        sql = @"SELECT * FROM activity_quiz_prize WHERE prize_id = @prize_id";
                        var findData = await conn.QueryAsync(sql, new { prize_id = (string)webActivityData[prize_id] });
                        if (findData.Any())
                        {
                            sql = @"DELETE FROM activity_quiz_prize WHERE prize_id = @prize_id";
                            await conn.QueryAsync(sql, new { prize_id = (string)webActivityData[prize_id] });
                        }
                        else
                        {
                            break;
                        }

                    }
                }
            }

            return webActivityID;
        }

        public async Task<int> InsertActivity(JObject webActivityData)
        {
            int activity_id = 0;
            using (var conn = _context.CreateConnection2())
            {
                string start = (string)webActivityData["time"][0];
                string end = (string)webActivityData["time"][1];

                long start_time = DateTimeOffset.Parse(start).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                long end_time = DateTimeOffset.Parse(end).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();

                int icon_fid = 0;

                if (webActivityData["Activity_image"] != null) { icon_fid = await _filesService.UploadFilesToGCS(webActivityData, "image", "webactivity"); }

                string sql = @"INSERT INTO activity
                        (activity_type,title,start_time,end_time,chance,play_interval,play_target,
                        is_free,icon_fid,push_icon_fid,push_title, status,deleted,created,modify)VALUES
                        (@activity_type,@title,@start_time,@end_time,@chance,@play_interval,@play_target,
                        @is_free,@icon_fid,@push_icon_fid,@push_title,@status,@deleted,@created,@modify);

                        SELECT CAST(LAST_INSERT_ID() as UNSIGNED INTEGER);";
                activity_id = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                {
                    activity_type = (string)webActivityData["activity_type"],
                    title = (string)webActivityData["title"],
                    start_time = start_time,
                    end_time = end_time,
                    chance = (int)webActivityData["chance"],
                    play_interval = (int)webActivityData["play_interval"],
                    play_target = (string)webActivityData["play_target"],
                    is_free = (int)webActivityData["is_free"],
                    icon_fid = icon_fid,
                    push_title = "",
                    push_icon_fid = 0,
                    status = 1,
                    deleted = 0,
                    created = today,
                    modify = 0
                });
            }
            return activity_id;
        }
    }
}
