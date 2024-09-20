namespace PKApp.ConfigOptions
{
    public class StatisicalNewsClass
    {
        public int? TotalMessage { get; set; }
        public int? TotalMember { get; set; }
        public int? TotalSend { get; set; }
        public int? TotalRead { get; set; }
        public float? Percent { get; set; }
        public List<dynamic>? SingleNewsData { get; set; }
    }

    public class StatisicalNewsData
    {
        public int? ID { get; set; }
        public string? Title { get; set; }
        public int? Member { get; set; }
        public int? Send { get; set; }
        public int? Open { get; set; }
        public float? Percent { get; set; }
    }
}
