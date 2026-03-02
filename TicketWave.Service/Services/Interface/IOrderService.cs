using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TicketWave.Repository.Entity;

namespace TicketWave.Service.Services.Interface
{
    public interface IOrderService
    {
        /// <summary>
        /// 查詢會員的所有訂單
        /// </summary>
        Task<List<Order>> GetMemberOrders(Guid memberId);

        /// <summary>
        /// 查詢訂單詳情（包含座位資訊）
        /// </summary>
        Task<Order> GetOrderById(Guid orderId);

        /// <summary>
        /// 檢查會員在特定演唱會的購票數量
        /// </summary>
        /// <param name="memberId">會員 ID</param>
        /// <param name="concertId">演唱會 ID</param>
        /// <returns>已購買的票數</returns>
        Task<int> GetMemberTicketCountForConcert(Guid memberId, Guid concertId);

        /// <summary>
        /// 驗證是否可以購買（檢查是否超過 4 張限制）
        /// </summary>
        /// <param name="memberId">會員 ID</param>
        /// <param name="concertId">演唱會 ID</param>
        /// <param name="requestTicketCount">想購買的票數</param>
        /// <returns>是否可以購買</returns>
        Task<(bool CanPurchase, string Message, int CurrentCount)> ValidatePurchaseLimit(
            Guid memberId, Guid concertId, int requestTicketCount);

        /// <summary>
        /// 建立訂單
        /// </summary>
        /// <param name="memberId">會員 ID</param>
        /// <param name="concertId">演唱會 ID</param>
        /// <param name="seatIds">選擇的座位 ID 列表</param>
        /// <returns>訂單 ID</returns>
        Task<(bool Success, string Message, Guid? OrderId)> CreateOrder(
            Guid memberId, Guid concertId, List<Guid> seatIds);

        /// <summary>
        /// 取消訂單
        /// </summary>
        Task<bool> CancelOrder(Guid orderId, Guid memberId);
    }
}
