using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TicketWave.Repository.Entity;

namespace TicketWave.Repository.Repositories.Interface
{
    public interface IMemberRepository
    {
        //取得所有會員資料(只取未刪除會員)
        Task<List<Member>> GetAll();

        //取得所有會員資料(包含已刪除會員)，供後台管理者使用
        Task<List<Member>> GetAllIncludingDeleted();


        //軟刪除會員
        Task SoftDelete(Guid memberId);

        //恢復已刪除會員
        Task Restore(Guid memberId);

        //根據會員ID取得單一會員資料(包含已刪除的)，供後台管理者使用
        Task<Member?>GetByIdIncludingDeleted(Guid memberId);

        //根據會員Email取得單一會員資料(包含已刪除的)，供後台管理者使用
        Task<Member?> GetByEmailIncludingDeleted(string email);

        //取得已刪除會員資料，供後台管理者使用
        Task<List<Member>> GetDeletedMembers();

        //新增會員
        Task Add(Member member);

        //更新會員資料
        Task Update(Member member);

        //硬刪除會員(實際刪除資料庫紀錄)
        Task Delete(Member member);

        //根據會員ID取得單一會員資料(只取未刪除會員)
        Task<Member?> GetById(Guid memberId);

        Task<bool> CheckByNationalID(string nationalID);
        Task<bool> CheckByEmail(string email);
        Task<Member> GetByEmail(string email);
    }
}
