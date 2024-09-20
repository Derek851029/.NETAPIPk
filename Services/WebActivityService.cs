using Newtonsoft.Json.Linq;
using PKApp.Tools;

namespace PKApp.Services
{
    public interface IWebActivityService
    {
        Task<int> HandleDB(string type, JObject webActivityData, int webActivityID = 0);
    }
    public class WebActivityService : IWebActivityService
    {
        private readonly WebActivityTool _tool;
        public WebActivityService(WebActivityTool webActivityTool)
        {
            _tool = webActivityTool;
        }
        public async Task<int> HandleDB(string type, JObject webActivityData, int webActivityID)
        {
            string activityType = (string)webActivityData["activity_type"];

            if (webActivityID == 0)
            {
                if (activityType == "DrawPrize")
                {
                    webActivityID = await _tool.InsertDrawPrize(webActivityData);
                }
                else if (activityType == "Slots")
                {
                    webActivityID = await _tool.InsertSlots(webActivityData);
                }
                else if (activityType == "Quiz")
                {
                    webActivityID = await _tool.InsertQuiz(webActivityData);
                }

            }
            else
            {
                if (activityType == "DrawPrize")
                {
                    webActivityID = await _tool.UpdateDrawPrize(webActivityData, webActivityID);
                }
                else if (activityType == "Slots")
                {
                    webActivityID = await _tool.UpdateSlots(webActivityData, webActivityID);
                }
                else if (activityType == "Quiz")
                {
                    webActivityID = await _tool.UpdateQuiz(webActivityData, webActivityID);
                }

            }
            return webActivityID;
        }
    }
}
