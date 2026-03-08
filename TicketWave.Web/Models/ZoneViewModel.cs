namespace TicketWave.Web.Models
{
    /// <summary>
    /// 座位區域 ViewModel
    /// 用於顯示各區域的售票狀況
    /// </summary>
    public class ZoneViewModel
    {
        /// <summary>
        /// 區域名稱
        /// </summary>
        public string ZoneName { get; set; }

        /// <summary>
        /// 總座位數
        /// </summary>
        public int TotalSeats { get; set; }

        /// <summary>
        /// 剩餘座位數
        /// </summary>
        public int AvailableSeats { get; set; }

        /// <summary>
        /// 已售出座位數
        /// </summary>
        public int SoldSeats { get; set; }

        /// <summary>
        /// 最低票價
        /// </summary>
        public decimal MinPrice { get; set; }

        /// <summary>
        /// 最高票價
        /// </summary>
        public decimal MaxPrice { get; set; }

        /// <summary>
        /// 是否已售罄
        /// </summary>
        public bool IsSoldOut => AvailableSeats == 0;

        /// <summary>
        /// 售出百分比
        /// </summary>
        public int SoldPercentage => TotalSeats > 0
            ? (int)((SoldSeats * 100.0) / TotalSeats)
            : 0;
    }
}
