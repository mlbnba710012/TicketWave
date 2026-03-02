using TicketWave.Repository.Repositories.Implement;
using TicketWave.Repository.Repositories.Interface;
using TicketWave.Service.Services.Implement;
using TicketWave.Service.Services.Interface;

namespace TicketWave.Web.Extensions
{
    public static class FeatureServicesExtensions
    {
        public static IServiceCollection AddFeatureServices(this IServiceCollection services)
        {
            #region 註冊services.

            services.AddScoped<IMemberService, MemberService>();
            services.AddScoped<IOrderService, OrderService>();
            #endregion






            #region 註冊 Repositories

            services.AddScoped<IMemberRepository, MemberRepository>();

            #endregion

            #region 註冊repositories.
            services.AddScoped<IMemberRepository, MemberRepository>();

            #endregion

            return services;
        }
    }
}
