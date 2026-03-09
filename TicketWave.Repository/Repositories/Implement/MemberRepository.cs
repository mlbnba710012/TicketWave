using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TicketWave.Repository.Entity;
using TicketWave.Repository.Repositories.Interface;


namespace TicketWave.Repository.Repositories.Implement
{
    public class MemberRepository : IMemberRepository
    {
        private readonly TicketWaveContext _dbContext;
        public MemberRepository(TicketWaveContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Member>> GetAll()
        {
            return await _dbContext.Members.AsNoTracking().ToListAsync();
        }

        public async Task Add(Member member)
        {
            await _dbContext.Members.AddAsync(member);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Update(Member member)
        {
            _dbContext.Members.Update(member);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Delete(Member member)
        {
            _dbContext.Members.Remove(member);
            await _dbContext.SaveChangesAsync();            
        }

        public async Task<Member?> GetById(Guid memberId)
        {
            return await _dbContext.Members.FindAsync(memberId);
        }

        public async Task<bool> CheckByEmail(string email)
        {
            return await _dbContext.Members.CountAsync(m => m.Email == email) == 0;
        }

        public async Task<bool> CheckByNationalID(string nationalID)
        {
            return await _dbContext.Members.CountAsync(m => m.NationalID == nationalID) == 0;
        }

        public async Task<Member> GetByEmail(string email)
        {
            return await _dbContext.Members.FirstOrDefaultAsync(m => m.Email == email);
        }
    }
}
