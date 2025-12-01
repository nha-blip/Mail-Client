using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mailclient
{
    public class ParsedEmail
    {
        public string MessageId { get; set; }
        public string Subject { get; set; }
        public string From { get; set; }
        public List<string> To { get; set; }
        public DateTimeOffset Date { get; set; }
        public string BodyAsHtml { get; set; }
        public string BodyAsText { get; set; }
        public List<ParsedAttachmentInfo> Attachments { get; set; }

        public ParsedEmail()
        {
            To = new List<string>();
            Attachments = new List<ParsedAttachmentInfo>();
        }
    }
}
