using PKApp.ConfigOptions;
using PKApp.Tools;

namespace PKApp.Services
{
    public interface IStatisticalService
    {
        Task<StatisicalNewsClass> HandleDB(string type, int start, int end, string brand);
        Task<string> DynamicReport();
    }

    public class StatisticalService : IStatisticalService
    {
        private readonly AppNewsStatisticalTool _appNewsStatisticalTool;
        private readonly IFirebaseService _firebaseService;
        public StatisticalService(AppNewsStatisticalTool appNewsStatisticalTool, IFirebaseService firebaseService)
        {
            _appNewsStatisticalTool = appNewsStatisticalTool;
            _firebaseService = firebaseService;
        }

        public Task<StatisicalNewsClass> HandleDB(string type, int start, int end, string brand)
        {
            dynamic data = null;
            switch (type)
            {
                case "newsReadMonth":
                    data = _appNewsStatisticalTool.AppNewsTotalData(start, end, brand);
                    break;
            }

            return data;
        }

        public async Task<string> DynamicReport()
        {
            await _firebaseService.GetDynamicReport();
            return "";
        }
    }
}
