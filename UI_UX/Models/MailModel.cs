﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailClient.Core.Models
{
    public class MailModel
    {
        public String Id { get; set; } = String.Empty;
        public String From { get; set; } = String.Empty;
        public List<String> To { get; set; } = new List<String>();
        public List<String> Cc { get; set; } = new List<String>();
        public List<String> Bcc { get; set; } = new List<String>();
        public String Subject { get; set; } = String.Empty;
        public String HtmlBody { get; set; } = String.Empty;
        public String TextBody { get; set; } = String.Empty;
        public DateTime Date { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
        // Attachments property should hold the full file path strings
        public List<String> Attachments { get; set; } = new List<String>();
    }
}
