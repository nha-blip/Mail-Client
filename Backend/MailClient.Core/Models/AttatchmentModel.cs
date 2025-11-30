using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MailClient.Core.Models
{
    public class AttatchmentModel
    {
        public String FileName { get; set; } = String.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>(); // Can get size from this
        public String MimeType { get; set; } = String.Empty;
    }
}
