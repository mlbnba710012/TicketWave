using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketWave.Web.Models.Parameter
{
    public class RegisterParameter
    {
        [Required(ErrorMessage ="請輸入身分證字號")]
        [RegularExpression(@"^[A-Z][12]\d{8}$",ErrorMessage = "身分證字號格式不正確")]
        public string NationalID { get; set; }

        [Required(ErrorMessage = "請輸入手機")]
        [RegularExpression(@"^09\d{8}$",ErrorMessage = "手機號碼格式不正確")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "請輸入Email")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",ErrorMessage = "Email 格式不正確")]
        public string Email { get; set; }

        [Required(ErrorMessage = "請輸入密碼")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{6,}$",ErrorMessage = "密碼必須包含至少一個字母和一個數字")]
        public string Password { get; set; }

        [Required(ErrorMessage = "請確認密碼")]
        [Compare("Password", ErrorMessage = "兩次輸入的密碼不相同")]
        public string ConfirmPassword { get; set; } 


    }
}
