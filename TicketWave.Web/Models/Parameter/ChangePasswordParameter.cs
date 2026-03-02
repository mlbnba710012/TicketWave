using System.ComponentModel.DataAnnotations;

namespace TicketWave.Web.Models.Parameter
{
    public class ChangePasswordParameter
    {
        [Required(ErrorMessage = "請輸入目前密碼")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "請輸入新密碼")]
        //[StringLength(50, MinimumLength = 6, ErrorMessage = "密碼長度必須介於 6-50 字元")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "請確認新密碼")]
        [Compare("NewPassword", ErrorMessage = "兩次輸入的新密碼不相符")]
        public string ConfirmPassword { get; set; }

    }
}
