using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TicketWave.Repository.Entity;

namespace TicketWave.Repository.Repositories.Interface
{
    public interface IMemberRepository
    {
        Task<List<Member>> GetAll();

        Task Add(Member member);

        Task Update(Member member);

        Task Delete(Member member);
        Task<Member?> GetById(Guid memberId);

        Task<bool> CheckByNationalID(string nationalID);
        Task<bool> CheckByEmail(string email);
        Task<Member> GetByEmail(string email);
    }
}
