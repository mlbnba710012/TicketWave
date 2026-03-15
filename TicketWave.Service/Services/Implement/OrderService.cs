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
        private const int MAX_TICKETS_PER_EVENT = 4; // 每場活動最多 4 張票

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
        /// 檢查會員在特定活動已購買的票數
        /// </summary>
        public async Task<int> GetMemberTicketCountForEvent(Guid memberId, Guid eventId, string eventType)
        {
            var query = _dbContext.Orders
                .Where(o => o.MemberId == memberId && o.OrderStatus != 2); // 排除已取消

            var ticketCount = eventType.ToLower() switch
            {
                "concert" => await query.Where(o => o.ConcertId == eventId).SumAsync(o => o.TicketCount),
                "sport" => await query.Where(o => o.SportId == eventId).SumAsync(o => o.TicketCount),
                "theater" => await query.Where(o => o.TheaterId == eventId).SumAsync(o => o.TicketCount),
                _ => throw new ArgumentException($"不支援的活動類型：{eventType}")
            };

            return ticketCount;
        }

        /// <summary>
        /// 驗證購票限制（每場活動最多 4 張）
        /// </summary>
        public async Task<(bool CanPurchase, string Message, int CurrentCount)> ValidatePurchaseLimit(
            Guid memberId, Guid eventId, string eventType, int requestTicketCount)
        {
            if (requestTicketCount < 1)
                return (false, "至少需要選擇 1 個座位", 0);

            if (requestTicketCount > MAX_TICKETS_PER_EVENT)
                return (false, $"單次最多只能選擇 {MAX_TICKETS_PER_EVENT} 個座位", 0);

            var currentCount = await GetMemberTicketCountForEvent(memberId, eventId, eventType);
            var totalAfterPurchase = currentCount + requestTicketCount;

            if (totalAfterPurchase > MAX_TICKETS_PER_EVENT)
            {
                var remainingQuota = MAX_TICKETS_PER_EVENT - currentCount;
                return (false,
                    $"每場活動最多只能購買 {MAX_TICKETS_PER_EVENT} 張票。您已購買 {currentCount} 張，還可購買 {remainingQuota} 張。",
                    currentCount);
            }

            return (true, "可以購買", currentCount);
        }

        /// <summary>
        /// 建立演唱會訂單
        /// </summary>
        public async Task<(bool Success, string Message, Guid? OrderId)> CreateConcertOrder(
            Guid memberId, Guid concertId, List<Guid> seatIds)
        {
            var concert = await _dbContext.Concerts.FindAsync(concertId);
            if (concert == null)
                return (false, "演唱會不存在", null);
            if (concert.Status != 1)
                return (false, "此演唱會目前未開放售票", null);

            return await CreateOrderInternal(
                memberId: memberId,
                eventName: concert.ConcertName,
                seatIds: seatIds,
                eventType: "concert",
                eventId: concertId,
                updateAvailableSeats: (count) =>
                {
                    concert.AvailableSeats -= count;
                    concert.UpdateDate = DateTime.Now;
                },
                buildOrder: (orderId, orderNumber, now, totalAmount) => new Order
                {
                    OrderId = orderId,
                    OrderNumber = orderNumber,
                    MemberId = memberId,
                    ConcertId = concertId,
                    EventName = concert.ConcertName,
                    TotalAmount = totalAmount,
                    TicketCount = seatIds.Count,
                    OrderStatus = 0,
                    CreateDate = now,
                    UpdateDate = now
                }
            );
        }

        /// <summary>
        /// 建立運動賽事訂單
        /// </summary>
        public async Task<(bool Success, string Message, Guid? OrderId)> CreateSportOrder(
            Guid memberId, Guid sportId, List<Guid> seatIds)
        {
            var sport = await _dbContext.Sports.FindAsync(sportId);
            if (sport == null)
                return (false, "運動賽事不存在", null);
            if (sport.Status != 1)
                return (false, "此運動賽事目前未開放售票", null);

            return await CreateOrderInternal(
                memberId: memberId,
                eventName: sport.SportName,
                seatIds: seatIds,
                eventType: "sport",
                eventId: sportId,
                updateAvailableSeats: (count) =>
                {
                    sport.AvailableSeats -= count;
                    sport.UpdateDate = DateTime.Now;
                },
                buildOrder: (orderId, orderNumber, now, totalAmount) => new Order
                {
                    OrderId = orderId,
                    OrderNumber = orderNumber,
                    MemberId = memberId,
                    SportId = sportId,
                    EventName = sport.SportName,
                    TotalAmount = totalAmount,
                    TicketCount = seatIds.Count,
                    OrderStatus = 0,
                    CreateDate = now,
                    UpdateDate = now
                }
            );
        }

        /// <summary>
        /// 建立表演藝術訂單
        /// </summary>
        public async Task<(bool Success, string Message, Guid? OrderId)> CreateTheaterOrder(
            Guid memberId, Guid theaterId, List<Guid> seatIds)
        {
            var theater = await _dbContext.Theaters.FindAsync(theaterId);
            if (theater == null)
                return (false, "表演藝術活動不存在", null);
            if (theater.Status != 1)
                return (false, "此表演藝術活動目前未開放售票", null);

            return await CreateOrderInternal(
                memberId: memberId,
                eventName: theater.TheaterName,
                seatIds: seatIds,
                eventType: "theater",
                eventId: theaterId,
                updateAvailableSeats: (count) =>
                {
                    theater.AvailableSeats -= count;
                    theater.UpdateDate = DateTime.Now;
                },
                buildOrder: (orderId, orderNumber, now, totalAmount) => new Order
                {
                    OrderId = orderId,
                    OrderNumber = orderNumber,
                    MemberId = memberId,
                    TheaterId = theaterId,
                    EventName = theater.TheaterName,
                    TotalAmount = totalAmount,
                    TicketCount = seatIds.Count,
                    OrderStatus = 0,
                    CreateDate = now,
                    UpdateDate = now
                }
            );
        }

        /// <summary>
        /// 建立訂單的共用內部邏輯
        /// </summary>
        private async Task<(bool Success, string Message, Guid? OrderId)> CreateOrderInternal(
            Guid memberId,
            string eventName,
            List<Guid> seatIds,
            string eventType,
            Guid eventId,
            Action<int> updateAvailableSeats,
            Func<Guid, string, DateTime, decimal, Order> buildOrder)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // 1. 驗證購票限制
                var validation = await ValidatePurchaseLimit(memberId, eventId, eventType, seatIds.Count);
                if (!validation.CanPurchase)
                    return (false, validation.Message, null);

                // 2. 查詢座位並確認狀態
                var seats = await _dbContext.Seats
                    .Where(s => seatIds.Contains(s.SeatId))
                    .ToListAsync();

                if (seats.Count != seatIds.Count)
                    return (false, "部分座位不存在", null);

                var unavailableSeats = seats.Where(s => s.SeatStatus != 0).ToList();
                if (unavailableSeats.Any())
                {
                    var seatInfo = string.Join(", ", unavailableSeats.Select(s => $"{s.SeatZone}{s.SeatRow}排{s.SeatNumber}號"));
                    return (false, $"以下座位已被購買或鎖定：{seatInfo}", null);
                }

                // 3. 建立訂單
                var orderId = Guid.NewGuid();
                var now = DateTime.Now;
                var orderNumber = GenerateOrderNumber();
                var totalAmount = seats.Sum(s => s.Price);

                var order = buildOrder(orderId, orderNumber, now, totalAmount);
                _dbContext.Orders.Add(order);

                // 4. 建立訂單明細並更新座位狀態
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
                        TicketStatus = 0,
                        CreateDate = now
                    };

                    _dbContext.OrderDetails.Add(orderDetail);

                    seat.SeatStatus = 1; // 已售出
                    seat.OrderDetailId = orderDetail.OrderDetailId;
                    seat.UpdateDate = now;
                }

                // 5. 更新活動剩餘座位數
                updateAvailableSeats(seats.Count);

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

                if (order == null || order.OrderStatus != 0)
                    return false;

                // 更新訂單狀態為已取消
                order.OrderStatus = 2;
                order.UpdateDate = DateTime.Now;

                // 用 OrderDetailId 直接找座位並釋放（支援三種活動）
                foreach (var detail in order.OrderDetails)
                {
                    var seat = await _dbContext.Seats
                        .FirstOrDefaultAsync(s => s.OrderDetailId == detail.OrderDetailId);

                    if (seat != null)
                    {
                        seat.SeatStatus = 0; // 可購買
                        seat.OrderDetailId = null;
                        seat.UpdateDate = DateTime.Now;
                    }
                }

                // 更新對應活動的剩餘座位數
                if (order.ConcertId.HasValue)
                {
                    var concert = await _dbContext.Concerts.FindAsync(order.ConcertId);
                    if (concert != null) { concert.AvailableSeats += order.TicketCount; concert.UpdateDate = DateTime.Now; }
                }
                else if (order.SportId.HasValue)
                {
                    var sport = await _dbContext.Sports.FindAsync(order.SportId);
                    if (sport != null) { sport.AvailableSeats += order.TicketCount; sport.UpdateDate = DateTime.Now; }
                }
                else if (order.TheaterId.HasValue)
                {
                    var theater = await _dbContext.Theaters.FindAsync(order.TheaterId);
                    if (theater != null) { theater.AvailableSeats += order.TicketCount; theater.UpdateDate = DateTime.Now; }
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
        /// 生成訂單編號（使用 Guid 片段避免碰撞）
        /// </summary>
        private string GenerateOrderNumber()
        {
            var date = DateTime.Now.ToString("yyyyMMdd");
            var unique = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            return $"ORD{date}{unique}";
        }
    }
}
