﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailClient.Core.Models
{
    public class EmailAccount
    {
        public String Address { get; set; } = String.Empty;
        public String DisplayName { get; set; } = String.Empty;
        public String SmtpServer { get; set; } = String.Empty;
        public int SmtpPort { get; set; } = 587; // for STARTTLS
        public String ImapServer { get; set; } = String.Empty;
        public int ImapPort { get; set; } = 993;
        public String Password { get; set; } = String.Empty;
    }
}
