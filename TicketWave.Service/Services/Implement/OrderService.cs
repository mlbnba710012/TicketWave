using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TicketWave.Repository.Entity;
using TicketWave.Service.Services.Interface;

namespace TicketWave.Service.Services.Implement
{
    public class OrderService : IOrderService
    {
        private readonly TicketWaveContext _dbContext;
        private const int MAX_TICKETS_PER_CONCERT = 4; // 每場演唱會最多 4 張票

        public OrderService(TicketWaveContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 查詢會員的所有訂單
        /// </summary>
        public async Task<List<Order>> GetMemberOrders(Guid memberId)
        {
            return await _dbContext.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.MemberId == memberId)
                .OrderByDescending(o => o.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// 查詢訂單詳情
        /// </summary>
        public async Task<Order> GetOrderById(Guid orderId)
        {
            return await _dbContext.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.Member)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        /// <summary>
        /// 檢查會員在特定演唱會已購買的票數
        /// </summary>
        public async Task<int> GetMemberTicketCountForConcert(Guid memberId, Guid concertId)
        {
            // 計算該會員在該場演唱會已購買且未取消的票數
            var ticketCount = await _dbContext.Orders
                .Where(o => o.MemberId == memberId
                         && o.ConcertId == concertId
                         && o.OrderStatus != 2) // 排除已取消的訂單
                .SumAsync(o => o.TicketCount);

            return ticketCount;
        }

        /// <summary>
        /// 驗證購票限制（每場演唱會最多 4 張）
        /// </summary>
        public async Task<(bool CanPurchase, string Message, int CurrentCount)> ValidatePurchaseLimit(
            Guid memberId, Guid concertId, int requestTicketCount)
        {
            // 查詢已購買的票數
            var currentCount = await GetMemberTicketCountForConcert(memberId, concertId);

            // 計算購買後的總票數
            var totalAfterPurchase = currentCount + requestTicketCount;

            if (totalAfterPurchase > MAX_TICKETS_PER_CONCERT)
            {
                var remainingQuota = MAX_TICKETS_PER_CONCERT - currentCount;
                return (false,
                    $"每場演唱會最多只能購買 {MAX_TICKETS_PER_CONCERT} 張票。您已購買 {currentCount} 張，還可購買 {remainingQuota} 張。",
                    currentCount);
            }

            if (requestTicketCount < 1)
            {
                return (false, "至少需要選擇 1 個座位", currentCount);
            }

            if (requestTicketCount > 4)
            {
                return (false, "單次最多只能選擇 4 個座位", currentCount);
            }

            return (true, "可以購買", currentCount);
        }

        /// <summary>
        /// 建立訂單
        /// </summary>
        public async Task<(bool Success, string Message, Guid? OrderId)> CreateOrder(
            Guid memberId, Guid concertId, List<Guid> seatIds)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // 1. 驗證購票限制
                var validation = await ValidatePurchaseLimit(memberId, concertId, seatIds.Count);
                if (!validation.CanPurchase)
                {
                    return (false, validation.Message, null);
                }

                // 2. 查詢座位資訊並鎖定
                var seats = await _dbContext.Seats
                    .Where(s => seatIds.Contains(s.SeatId) && s.ConcertId == concertId)
                    .ToListAsync();

                // 驗證座位數量
                if (seats.Count != seatIds.Count)
                {
                    return (false, "部分座位不存在", null);
                }

                // 檢查座位是否都可購買
                var unavailableSeats = seats.Where(s => s.SeatStatus != 0).ToList();
                if (unavailableSeats.Any())
                {
                    var seatInfo = string.Join(", ", unavailableSeats.Select(s => $"{s.SeatZone}{s.SeatRow}排{s.SeatNumber}號"));
                    return (false, $"以下座位已被購買或鎖定：{seatInfo}", null);
                }

                // 3. 查詢演唱會資訊
                var concert = await _dbContext.Concerts.FindAsync(concertId);
                if (concert == null)
                {
                    return (false, "演唱會不存在", null);
                }

                if (concert.Status != 1)
                {
                    return (false, "此演唱會目前未開放售票", null);
                }

                // 4. 建立訂單
                var orderId = Guid.NewGuid();
                var now = DateTime.Now;
                var orderNumber = GenerateOrderNumber();

                var order = new Order
                {
                    OrderId = orderId,
                    OrderNumber = orderNumber,
                    MemberId = memberId,
                    ConcertId = concertId,
                    ConcertName = concert.ConcertName,
                    TotalAmount = seats.Sum(s => s.Price),
                    TicketCount = seats.Count,
                    OrderStatus = 0, // 待付款
                    CreateDate = now,
                    UpdateDate = now
                };

                _dbContext.Orders.Add(order);

                // 5. 建立訂單明細
                foreach (var seat in seats)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderDetailId = Guid.NewGuid(),
                        OrderId = orderId,
                        SeatZone = seat.SeatZone,
                        SeatRow = seat.SeatRow,
                        SeatNumber = seat.SeatNumber,
                        Price = seat.Price,
                        TicketStatus = 0, // 未使用
                        CreateDate = now
                    };

                    _dbContext.OrderDetails.Add(orderDetail);

                    // 6. 更新座位狀態為已售出
                    seat.SeatStatus = 1; // 已售出
                    seat.OrderDetailId = orderDetail.OrderDetailId;
                    seat.UpdateDate = now;
                }

                // 7. 更新演唱會剩餘座位數
                concert.AvailableSeats -= seats.Count;
                concert.UpdateDate = now;

                // 8. 儲存變更
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "訂單建立成功", orderId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"訂單建立失敗：{ex.Message}", null);
            }
        }

        /// <summary>
        /// 取消訂單
        /// </summary>
        public async Task<bool> CancelOrder(Guid orderId, Guid memberId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var order = await _dbContext.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId && o.MemberId == memberId);

                if (order == null)
                {
                    return false;
                }

                // 只能取消待付款的訂單
                if (order.OrderStatus != 0)
                {
                    return false;
                }

                // 更新訂單狀態
                order.OrderStatus = 2; // 已取消
                order.UpdateDate = DateTime.Now;

                // 釋放座位
                foreach (var detail in order.OrderDetails)
                {
                    var seat = await _dbContext.Seats
                        .FirstOrDefaultAsync(s => s.ConcertId == order.ConcertId
                                                && s.SeatZone == detail.SeatZone
                                                && s.SeatRow == detail.SeatRow
                                                && s.SeatNumber == detail.SeatNumber);

                    if (seat != null)
                    {
                        seat.SeatStatus = 0; // 可購買
                        seat.OrderDetailId = null;
                        seat.UpdateDate = DateTime.Now;
                    }
                }

                // 更新演唱會剩餘座位數
                var concert = await _dbContext.Concerts.FindAsync(order.ConcertId);
                if (concert != null)
                {
                    concert.AvailableSeats += order.TicketCount;
                    concert.UpdateDate = DateTime.Now;
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        /// <summary>
        /// 生成訂單編號
        /// </summary>
        private string GenerateOrderNumber()
        {
            var date = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random().Next(1000, 9999);
            return $"ORD{date}{random}";
        }
    }
}
