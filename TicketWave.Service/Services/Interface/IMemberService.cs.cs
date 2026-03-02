using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TicketWave.Repository.Entity;
using TicketWave.Service.Models.Info;

namespace TicketWave.Service.Services.Interface
{
    public interface IMemberService
    {
        Task<List<Member>> GetAll();
        Task<(bool Success, string Message)> Register(RegisterInfo info);

        Task<bool> Login(string email, string password);
        Task<bool> UpdateMemberProfile(UpdateMemberProfileInfo member);
        Task<Member?> GetById(Guid memberId);
        Task<Member?> GetByEmail(string email);

        Task<(bool Success, string Message)> ChangePassword(ChangePasswordInfo info);
        Task<(bool Success, string Message)> DeleteAccount(Guid memberId, string password);
        Task<bool> DeleteMember(Guid memberId);
        Task Logout();
        
    }
}
