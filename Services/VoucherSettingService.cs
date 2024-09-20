using PKApp.Tools;

namespace PKApp.Services
{
    public interface IVoucherSettingService
    {
        Task<int> HandleDBAsync(VoucherData voucherData, int voucherID, string aid);
    }

    public class VoucherSettingService : IVoucherSettingService
    {
        private readonly VoucherSettingTool _tool;
        public VoucherSettingService(VoucherSettingTool voucherTool)
        {
            _tool = voucherTool;
        }

        public async Task<int> HandleDBAsync(VoucherData voucherData, int voucherID, string aid)
        {
            if (voucherID == 0)
            {
                voucherID = await _tool.Insert(voucherData, aid);
            }
            else
            {
                voucherID = await _tool.Update(voucherData, voucherID, aid);
            }

            return voucherID;
        }
    }
}
