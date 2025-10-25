using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailClient.Core.Models
{
    public class MailMessage
    {
        public String Id { get; set; } = String.Empty;
        public String From { get; set; } = String.Empty;
        public List<String> To { get; set; } = new List<String>();
        public String Subject { get; set; } = String.Empty;
        public String Body { get; set; } = String.Empty;
        public DateTime Date {  get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
    }
}
