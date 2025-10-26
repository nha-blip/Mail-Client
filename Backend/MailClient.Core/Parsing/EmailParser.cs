using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailClient.Core.Parsing
{
    public class EmailParser
    {
        public async Task<ParsedEmail> ParseAsync(MimeMessage rawMessage)
        {
            // Dùng Task.Run để đẩy việc nặng (parsing) ra luồng khác,
            // tránh làm "đơ" UI của Người A.
            return await Task.Run(() =>
            {
                var parsedEmail = new ParsedEmail();

                // 1. Trích xuất Headers (Thông tin cơ bản)
                parsedEmail.MessageId = rawMessage.MessageId;
                parsedEmail.Subject = rawMessage.Subject;
                parsedEmail.Date = rawMessage.Date;
                parsedEmail.From = rawMessage.From.ToString();
                parsedEmail.To.AddRange(rawMessage.To.Select(r => r.ToString()));

                // 2. Trích xuất Body và Attachments (Việc phức tạp)

                // Tạo "visitor" (từ file bạn vừa thêm ở Bước 3)
                // Sẽ không còn lỗi ở đây!
                var visitor = new HtmlPreviewVisitor();

                // Thả visitor vào MimeMessage để nó tự làm việc
                rawMessage.Accept(visitor);

                // Lấy HTML "sạch" (đã xử lý cid:) từ visitor
                parsedEmail.BodyAsHtml = visitor.HtmlBody;

                // Lấy danh sách file đính kèm "sạch" (đã lọc ảnh inline)
                foreach (var attachment in visitor.Attachments.OfType<MimePart>())
                {
                    var attachmentInfo = new ParsedAttachmentInfo
                    {
                        FileName = attachment.FileName,
                        SizeInBytes = attachment.Content?.Stream?.Length ?? 0,

                        // Lưu "chìa khóa bí mật" để dùng cho hàm Save
                        OriginalMimePart = attachment
                    };
                    parsedEmail.Attachments.Add(attachmentInfo);
                }

                return parsedEmail;
            });
        }

        public async Task SaveAttachmentAsync(ParsedAttachmentInfo attachmentInfo, string savePath)
        {
            // Dùng "chìa khóa bí mật" đã lưu để lấy data
            using (var stream = File.Create(savePath))
            {
                // Dùng hàm ...Async để không làm "đơ" UI khi lưu file lớn
                await attachmentInfo.OriginalMimePart.Content.DecodeToAsync(stream);
            }
        }
    }
}