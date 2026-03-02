using AutoMapper;
using TicketWave.Service.Models.Info;
using TicketWave.Web.Models.Parameter;

namespace TicketWave.Web.Profiles
{
    public class TicketWaveProfile : Profile
    {
        public TicketWaveProfile()
        {
            CreateMap<RegisterParameter, RegisterInfo>();
            CreateMap<UpdateMemberProfileParameter, UpdateMemberProfileInfo>();
        }


    }
}
