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
        //取得所有會員資料(只取未刪除會員)
        Task<List<Member>> GetAll();

        //取得所有會員資料(包含已刪除會員)，供後台使用
        Task<List<Member>> GetAllIncludingDeleted();

        //根據會員ID取得單一會員資料(只取未刪除的)
        Task<Member?>GetById(Guid memberId);

        //根據會員ID取得單一會員資料(包含已刪除的)，供後台使用
        Task<Member?> GetByIdIncludingDeleted(Guid memberId);

        //根據Email取得單一會員資料(只取未刪除的)
        Task<Member?> GetByEmail(string email);

        //根據Email取得單一會員資料(包含已刪除的)
        Task<Member?> GetByEmailIncludingDeleted(string email);

        //取得所有已刪除會員資料，供後台使用
        Task<List<Member>> GetDeletedMembers(); 

        //===========================
        // 會員認證相關
        //===========================

        //會員註冊
        Task<(bool Success, string Message)> Register(RegisterInfo info);

        //會員登入
        Task<(bool Success, string Message, Member? Member)> Login(string email, string password);

        //會員登出
        Task Logout();

        //===========================
        // 會員資料管理相關
        //===========================

        //會員資料更新
        Task<bool> UpdateMemberProfile(UpdateMemberProfileInfo member);

        //會員密碼變更
        Task<(bool Success, string Message)> ChangePassword(ChangePasswordInfo info);

        //============================
        //會員軟刪除相關(前台會員操作)
        //============================

        //會員自己刪除帳號(軟刪除)
        Task<(bool Success, string Message)> DeleteAccount(Guid memberId, string password);

        //管理者刪除會員帳號(軟刪除)
        Task<(bool Success, string Message)> SoftDeleteMember(Guid memberId); 

        //管理者恢復已刪除的會員帳號
        Task<(bool Success, string Message)> RestoreMember(Guid memberId);

        //============================
        //會員硬刪除相關(後台管理者操作)
        //============================

        //管理者硬刪除會員帳號(實際刪除資料庫紀錄)，請謹慎使用!
        Task<(bool Success, string Message)> PermanentDeleteMember(Guid memberId);

        //舊方法會員自己使用硬刪除會員帳號(實際刪除資料庫紀錄)，請謹慎使用!
        Task<bool> DeleteMember(Guid memberId);
        
        
    }
}
