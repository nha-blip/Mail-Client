using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mailclient
{
    public class ParsedAttachmentInfo
    {
        public string FileName { get; set; }
        public long SizeInBytes { get; set; }
        internal MimePart OriginalMimePart { get; set; }
    }
}
