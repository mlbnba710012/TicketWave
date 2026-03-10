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
        private readonly MemberDbContext _dbContext;
        public MemberRepository(MemberDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        //只取未刪除的會員
        public async Task<List<Member>> GetAll()
        {
            //return await _dbContext.Members.AsNoTracking().ToListAsync();
            return await _dbContext.Members.Where(m => !m.IsDelete).AsNoTracking().ToListAsync();
        }


        //取全部會員(包含已刪除)
        public async Task<List<Member>> GetAllIncludingDeleted()
        {
            return await _dbContext.Members.AsNoTracking().ToListAsync();
        }





        //新增會員
        public async Task Add(Member member)
        {
            
            member.IsDelete = false;
            await _dbContext.Members.AddAsync(member);
            await _dbContext.SaveChangesAsync();
        }

        //更新會員
        public async Task Update(Member member)
        {
            _dbContext.Members.Update(member);
            await _dbContext.SaveChangesAsync();
        }

        //硬刪除會員(實務上不建議)
        public async Task Delete(Member member)
        {
            _dbContext.Members.Remove(member);
            await _dbContext.SaveChangesAsync();            
        }

        //軟刪除會員
        public async Task SoftDelete(Guid memberId)
        {
            var member = await _dbContext.Members.FindAsync(memberId);
            if (member != null)
            {
                member.IsDelete = true;
                _dbContext.Members.Update(member);
                await _dbContext.SaveChangesAsync();
            }
        }

        //恢復已刪除會員
        public async Task Restore(Guid memberId)
        {
            var member = await _dbContext.Members.FindAsync(memberId);
            if ( member != null && member.IsDelete)
            {
                member.IsDelete = false;
                _dbContext.Members.Update(member);
                await _dbContext.SaveChangesAsync();
            }
        }

        //取得單一會員(不包含已刪除)
        public async Task<Member?> GetById(Guid memberId)
        {
            //return await _dbContext.Members.FindAsync(memberId);

            return await _dbContext.Members.Where(m => !m.IsDelete).FirstOrDefaultAsync(m => m.MemberId == memberId);
        }

        //取得單一會員(包含已刪除)，供後台使用
        public async Task<Member?> GetByIdIncludingDeleted(Guid memberId)
        {
            return await _dbContext.Members.FirstOrDefaultAsync(m => m.MemberId == memberId);
        }

        
        //檢查Email是否存在(不包含已刪除)
        public async Task<bool> CheckByEmail(string email)
        {
            //return await _dbContext.Members.CountAsync(m => m.Email == email) == 0;

            return await _dbContext.Members.Where(m => !m.IsDelete).CountAsync(m => m.Email == email) == 0;
        }

        //檢查身分證字號是否存在(不包含已刪除)
        public async Task<bool> CheckByNationalID(string nationalID)
        {
            //return await _dbContext.Members.CountAsync(m => m.NationalID == nationalID) == 0;

            return await _dbContext.Members.Where(m => !m.IsDelete).CountAsync(m => m.NationalID == nationalID) == 0;
        }

        //根據Email取得會員資料(不包含已刪除)
        public async Task<Member> GetByEmail(string email)
        {
            //return await _dbContext.Members.FirstOrDefaultAsync(m => m.Email == email);

            return await _dbContext.Members.Where(m => !m.IsDelete).FirstOrDefaultAsync(m => m.Email == email);
        }


        //透過Email取得會員，供登入時使用(包含已刪除，因為可能需要檢查已刪除帳號的登入嘗試)
        public async Task<Member?> GetByEmailIncludingDeleted(string email)
        {
            return await _dbContext.Members.FirstOrDefaultAsync(m => m.Email == email);
        }

        //取得以刪除會員清單，供後台管理者使用
        public async Task<List<Member>> GetDeletedMembers()
        {
            return await _dbContext.Members.Where(m => m.IsDelete).AsNoTracking().ToListAsync();
        }



    }
}
