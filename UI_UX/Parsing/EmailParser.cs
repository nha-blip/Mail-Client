using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mailclient
{
    public class EmailParser
    {
        // =========================================================================
        // PHẦN 1: LOGIC PARSING CHÍNH
        // =========================================================================

        public async Task<ParsedEmail> ParseAsync(MimeMessage rawMessage)
        {
            return await Task.Run(() =>
            {
                var parsedEmail = new ParsedEmail();

                // 1. Trích xuất Headers
                parsedEmail.MessageId = rawMessage.MessageId;
                parsedEmail.Subject = rawMessage.Subject;
                parsedEmail.Date = rawMessage.Date;
                parsedEmail.From = rawMessage.From.ToString();
                parsedEmail.To.AddRange(rawMessage.To.Select(r => r.ToString()));

                // 2. Trích xuất Body (HTML & Text)
                // Sử dụng HtmlPreviewVisitor để xử lý ảnh nội tuyến (cid:)
                var visitor = new HtmlPreviewVisitor();
                rawMessage.Accept(visitor);
                parsedEmail.BodyAsHtml = visitor.HtmlBody;

                // 3. Trích xuất Attachments (File đính kèm)
                foreach (var attachment in visitor.Attachments.OfType<MimePart>())
                {
                    var attachmentInfo = new ParsedAttachmentInfo
                    {
                        FileName = attachment.FileName,
                        SizeInBytes = attachment.Content?.Stream?.Length ?? 0,
                        OriginalMimePart = attachment // Lưu tham chiếu để save sau này
                    };
                    parsedEmail.Attachments.Add(attachmentInfo);
                }

                return parsedEmail;
            });
        }

        public async Task SaveAttachmentAsync(ParsedAttachmentInfo attachmentInfo, string savePath)
        {
            using (var stream = File.Create(savePath))
            {
                await attachmentInfo.OriginalMimePart.Content.DecodeToAsync(stream);
            }
        }

        // =========================================================================
        // PHẦN 2: TẠO GIAO DIỆN HTML (VIEW TEMPLATE - GMAIL STYLE)
        // =========================================================================

        public string GenerateDisplayHtml(ParsedEmail email, string customAvatarUrl = null)
        {
            // ... (Phần 1 và 2: Xử lý Header và Avatar GIỮ NGUYÊN) ...
            string senderName = GetSenderName(email.From);
            string senderEmail = GetSenderEmail(email.From);
            string dateString = email.Date.ToString("HH:mm, dd/MM/yyyy");

            string avatarHtml;
            if (!string.IsNullOrEmpty(customAvatarUrl))
            {
                avatarHtml = $@"<img class='avatar-img' src='{customAvatarUrl}' alt='AV' />";
            }
            else
            {
                string initials = GetInitials(senderName);
                string avatarColor = GetColorFromString(senderName);
                avatarHtml = $@"<div class='avatar-text' style='background-color: {avatarColor}'>{initials}</div>";
            }

            // ... (Phần 3: Xử lý Attachments GIỮ NGUYÊN) ...
            StringBuilder attachmentsHtml = new StringBuilder();
            if (email.Attachments.Count > 0)
            {
                attachmentsHtml.Append("<div class='attachments-area'>");
                attachmentsHtml.Append($"<div class='attachments-title'>{email.Attachments.Count} tệp đính kèm</div>");
                attachmentsHtml.Append("<div class='attachments-list'>");
                foreach (var att in email.Attachments)
                {
                    string size = FormatBytes(att.SizeInBytes);
                    attachmentsHtml.Append($@"
                        <div class='attachment-card' title='{att.FileName}'>
                            <div class='attachment-preview'>FILE</div>
                            <div class='attachment-footer'>
                                <div class='att-name'>{att.FileName}</div>
                                <div class='att-size'>{size}</div>
                            </div>
                        </div>");
                }
                attachmentsHtml.Append("</div></div>");
            }

            // *** GỌI HÀM LỌC NỘI DUNG TẠI ĐÂY ***
            string cleanBodyContent = ExtractBodyContent(email.BodyAsHtml);

            string template = $@"
            <!DOCTYPE html>
            <html lang='vi'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <style>
                    /* ... (Các CSS cũ giữ nguyên) ... */
                    body {{
                        font-family: 'Google Sans', Roboto, Helvetica, Arial, sans-serif;
                        background-color: #ffffff;
                        margin: 0;
                        padding: 20px;
                        color: #202124;
                        overflow-x: hidden;
                    }}
                    /* ... */

                    /* CẬP NHẬT CSS CHO EMAIL BODY ĐỂ TRÁNH VỠ KHUNG */
                    .email-body {{
                        font-size: 14px;
                        line-height: 1.5;
                        color: #202124;
                        margin-bottom: 30px;
                        padding-left: 56px;
                        
                        /* QUAN TRỌNG: Ngăn email con tràn ra ngoài hoặc phá vỡ layout */
                        overflow-wrap: break-word; 
                        word-wrap: break-word;
                        max-width: 100%;
                        overflow-x: auto; /* Nếu có bảng quá rộng, hiện thanh cuộn ngang thay vì vỡ layout */
                    }}
                    
                    /* Reset style cho các thành phần bên trong email con */
                    .email-body p {{ margin-bottom: 1em; }}
                    .email-body img {{ max-width: 100%; height: auto; }}

                    /* ... (Các CSS khác giữ nguyên) ... */
                    .subject-header {{ margin-bottom: 20px; border-bottom: 1px solid transparent; }}
                    .subject-text {{ font-size: 22px; font-weight: 400; margin: 0; line-height: 1.5; color: #1f1f1f; }}
                    .sender-header {{ display: flex; align-items: flex-start; margin-bottom: 20px; }}
                    .avatar-container {{ width: 40px; height: 40px; margin-right: 16px; flex-shrink: 0; }}
                    .avatar-img {{ width: 100%; height: 100%; border-radius: 50%; object-fit: cover; }}
                    .avatar-text {{ width: 100%; height: 100%; border-radius: 50%; color: white; display: flex; align-items: center; justify-content: center; font-size: 18px; font-weight: 500; }}
                    .sender-info {{ flex-grow: 1; display: flex; flex-direction: column; justify-content: center; }}
                    .sender-line-1 {{ display: flex; align-items: baseline; flex-wrap: wrap; }}
                    .sender-name {{ font-weight: 700; font-size: 14px; color: #202124; margin-right: 8px; }}
                    .sender-email {{ font-size: 12px; color: #5f6368; }}
                    .to-me {{ font-size: 12px; color: #5f6368; margin-top: 2px; }}
                    .email-date {{ color: #5f6368; font-size: 12px; margin-left: auto; white-space: nowrap; }}
                    .attachments-area {{ padding-left: 56px; margin-bottom: 30px; border-top: 1px solid #f1f3f4; padding-top: 15px; }}
                    .attachments-title {{ font-weight: 500; color: #5f6368; margin-bottom: 12px; font-size: 13px; }}
                    .attachments-list {{ display: flex; flex-wrap: wrap; gap: 12px; }}
                    .attachment-card {{ display: inline-flex; width: 180px; border: 1px solid #dadce0; border-radius: 8px; overflow: hidden; background-color: #f5f5f5; cursor: pointer; flex-direction: column; transition: box-shadow 0.2s; }}
                    .attachment-card:hover {{ box-shadow: 0 1px 3px rgba(0,0,0,0.2); border-color: #c0c2c5; }}
                    .attachment-preview {{ height: 90px; background-color: #e0e0e0; display: flex; align-items: center; justify-content: center; color: #888; font-size: 16px; font-weight: bold; letter-spacing: 1px; text-transform: uppercase; }}
                    .attachment-footer {{ background-color: white; padding: 10px; border-top: 1px solid #dadce0; }}
                    .att-name {{ font-size: 13px; font-weight: 500; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; color: #3c4043; margin-bottom: 2px; }}
                    .att-size {{ font-size: 11px; color: #5f6368; }}
                    .footer {{ margin-top: 40px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #888; text-align: center; }}
                    @media (max-width: 600px) {{ .email-body, .attachments-area {{ padding-left: 0; }} .avatar-container {{ display: none; }} }}
                </style>
            </head>
            <body>
                <div class='subject-header'>
                    <h1 class='subject-text'>{email.Subject}</h1>
                </div>

                <div class='sender-header'>
                    <div class='avatar-container'>{avatarHtml}</div>
                    <div class='sender-info'>
                        <div class='sender-line-1'>
                            <span class='sender-name'>{senderName}</span>
                            <span class='sender-email'>&lt;{senderEmail}&gt;</span>
                        </div>
                        <div class='to-me'>tới {string.Join(", ", email.To)}</div>
                    </div>
                    <div class='email-date'>{dateString}</div>
                </div>

                <div class='email-body'>
                    {cleanBodyContent}
                </div>

                {attachmentsHtml}

                <div class='footer'>
                    Hiển thị bởi Mail Client (Nhóm Đồ Án)
                </div>
            </body>
            </html>";

            return template;
        }

        // =========================================================================
        // PHẦN 3: CÁC HÀM HỖ TRỢ (HELPER METHODS)
        // =========================================================================

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }

        public string GetSenderName(string fullFromHeader)
        {
            int index = fullFromHeader.IndexOf('<');
            if (index > 0) return fullFromHeader.Substring(0, index).Trim(' ', '"');
            return fullFromHeader;
        }

        private string GetSenderEmail(string fullFromHeader)
        {
            int start = fullFromHeader.IndexOf('<');
            int end = fullFromHeader.IndexOf('>');
            if (start >= 0 && end > start) return fullFromHeader.Substring(start + 1, end - start - 1);
            return fullFromHeader;
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "??";
            var parts = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1 && parts[0].Length > 0) return parts[0].Substring(0, 1).ToUpper();
            if (parts.Length > 1) return (parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1)).ToUpper();
            return "??";
        }

        private string GetColorFromString(string name)
        {
            if (string.IsNullOrEmpty(name)) return "#0078D4";
            int hash = name.GetHashCode();
            // Bảng màu giống Gmail/Google Avatar
            string[] colors = { "#d93025", "#188038", "#1a73e8", "#e37400", "#673ab7", "#e91e63", "#0097a7", "#795548" };
            return colors[Math.Abs(hash) % colors.Length];
        }

        // --- HÀM MỚI ĐÃ NÂNG CẤP ---
        private string ExtractBodyContent(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";

            string content = html;

            // 1. Tìm nội dung bên trong thẻ <body> (nếu có)
            // RegexOptions.Singleline giúp dấu chấm (.) khớp với cả ký tự xuống dòng
            var match = Regex.Match(html, @"<body[^>]*>(.*?)</body>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (match.Success)
            {
                content = match.Groups[1].Value;
            }

            // 2. Xóa các thẻ HTML bao quanh gây lỗi giao diện (nếu còn sót lại)
            // Xóa <!DOCTYPE ...>
            content = Regex.Replace(content, @"<!DOCTYPE[^>]*>", "", RegexOptions.IgnoreCase);

            // Xóa <html> và </html>
            content = Regex.Replace(content, @"<html[^>]*>", "", RegexOptions.IgnoreCase);
            content = Regex.Replace(content, @"</html>", "", RegexOptions.IgnoreCase);

            // Xóa <head>...</head> nếu muốn (tùy chọn, nhưng thường email marketing để CSS trong head nên ta giữ lại)
            // Nếu bạn thấy CSS của email làm hỏng giao diện chính, hãy uncomment dòng dưới:
            // content = Regex.Replace(content, @"<head[^>]*>.*?</head>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            // Xóa thẻ body mở và đóng còn sót (trường hợp regex trên không bắt trọn)
            content = Regex.Replace(content, @"<body[^>]*>", "", RegexOptions.IgnoreCase);
            content = Regex.Replace(content, @"</body>", "", RegexOptions.IgnoreCase);

            return content.Trim();
        }
    }
}