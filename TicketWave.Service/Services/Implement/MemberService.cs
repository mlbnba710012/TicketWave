using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TicketWave.Repository.Entity;
using TicketWave.Repository.Repositories.Interface;
using TicketWave.Service.Models.Info;
using TicketWave.Service.Services.Interface;

namespace TicketWave.Service.Services.Implement
{
    public class MemberService : IMemberService
    {
        private readonly IMemberRepository _memberRepository;
        public MemberService(IMemberRepository memberRepository)
        {
            _memberRepository = memberRepository;
        }

        public async Task<List<Member>> GetAll()
        {
            var result = await _memberRepository.GetAll();

            return result;
        }

        public async Task<(bool Success, string Message)> Register(RegisterInfo info)
        {
            var blEmail = await _memberRepository.CheckByEmail(info.Email);
            if (!blEmail)
            {
                return (false, "此Email已被使用"); // Member with the same email already exists
            }
            //檢查身分證字號是否已被使用
            var blNationalID = await _memberRepository.CheckByNationalID(info.NationalID);
            if (!blNationalID)
            {
                return (false,"此身分證字號已被使用"); // 身分證字號已被使用
            }

            var now = DateTime.Now;

            var member = new Member
            {
                MemberId = Guid.NewGuid(),
                NationalID = info.NationalID,
                Phone = info.Phone,
                Email = info.Email,
                Password = info.Password,
                IsDelete = false,
                CreateDate = now,
                UpdateDate = now
            };

            await _memberRepository.Add(member);

            return (true,"註冊成功");
        }

        public async Task<bool> Login(string email, string password)
        {
            var member = await _memberRepository.GetByEmail(email);
            return member != null && member.Password == password;

            //var member = await _memberRepository.GetByEmail(email);
            //if (member == null || member.Password != password)
            //{
            //    return null; // Invalid email or password
            //}

            //return member;
        }

        public Task Logout()
        {
            // 你尚未做 Cookie/Session 登入狀態時，先留空即可
            return Task.CompletedTask;
        }

        public async Task<Member?> GetById(Guid memberId)
        {
            return await _memberRepository.GetById(memberId);
        }

        public async Task<Member?> GetByEmail(string email)
        {
            return await _memberRepository.GetByEmail(email);
        }

        public async Task<bool> UpdateMemberProfile(UpdateMemberProfileInfo info)
        {
            var member = await _memberRepository.GetById(info.MemberId);
            if (member == null) return false; // Member not found

            member.Name = info.Name;
            member.BirthDate = info.BirthDate;
            member.Address = info.Address;
            //member.Password = info.Password;
            member.Phone = info.Phone;
            member.UpdateDate = DateTime.Now;
            await _memberRepository.Update(member);

            return true;
        }

        /// <summary>
        /// 修改密碼
        /// </summary>
        public async Task<(bool Success, string Message)> ChangePassword(ChangePasswordInfo info)
        {
            // 1. 取得會員資料
            var member = await _memberRepository.GetById(info.MemberId);
            if (member == null)
            {
                return (false, "會員不存在");
            }

            // 2. 驗證舊密碼
            if (member.Password != info.OldPassword)
            {
                return (false, "目前密碼輸入錯誤");
            }

            // 3. 檢查新密碼不能與舊密碼相同
            if (info.OldPassword == info.NewPassword)
            {
                return (false, "新密碼不能與目前密碼相同");
            }

            // 4. 更新密碼
            member.Password = info.NewPassword;
            member.UpdateDate = DateTime.Now;
            await _memberRepository.Update(member);

            return (true, "密碼修改成功");
        }

        /// <summary>
        /// 刪除會員帳號
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteAccount(Guid memberId, string password)
        {
            // 1. 取得會員資料
            var member = await _memberRepository.GetById(memberId);
            if (member == null)
            {
                return (false, "會員不存在");
            }

            // 2. 驗證密碼（確保是本人操作）
            if (member.Password != password)
            {
                return (false, "密碼錯誤，無法刪除帳號");
            }

            // 3. 刪除會員
            //await _memberRepository.Delete(member);
            member.IsDelete = true; // 標記為刪除
            await _memberRepository.Update(member);

            return (true, "帳號已成功刪除");
        }


        public async Task<bool> DeleteMember(Guid memberId)
        {
            var member = await _memberRepository.GetById(memberId);
            if (member == null) return false; // Member not found

            await _memberRepository.Delete(member);
            return true;
        }
    }
}
