using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketWave.Service.Models.Info
{
    public class UpdateMemberProfileInfo
    {
        public Guid MemberId { get; set; }
        public string Name { get; set; }

        public string BirthDate { get; set; }

        public string Address { get; set; }

        public string Password { get; set; }
        public string Phone { get; set; }
    }
}
