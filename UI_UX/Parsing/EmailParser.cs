using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions; // Quan trọng để dùng Regex
using System.Threading.Tasks;
using MimeKit;


namespace Mailclient
{
    public class EmailParser
    {
        // =========================================================================
        // PHẦN 1: LOGIC PARSING CHÍNH
        // =========================================================================

        public async Task<MailClient.Email> ParseAsync(MimeMessage rawMessage)
        {
            return await Task.Run(() =>
            {
                var dbEmail = new MailClient.Email();

                // 1. Map Header
                dbEmail.Subject = rawMessage.Subject ?? "(No Subject)";
                dbEmail.From = GetSenderEmail(rawMessage.From.ToString());
                dbEmail.FromUser = GetSenderName(rawMessage.From.ToString());
                dbEmail.To = rawMessage.To.Select(m => m.ToString()).ToArray();
                dbEmail.DateSent = rawMessage.Date.DateTime;
                dbEmail.DateReceived = DateTime.Now;
                dbEmail.IsRead = false;

                //System.Diagnostics.Debug.WriteLine(rawMessage.From.ToString() + "\n");

                // 2. Map Body
                var visitor = new HtmlPreviewVisitor();
                rawMessage.Accept(visitor);
                dbEmail.BodyText = visitor.HtmlBody;

                // 3. Map Attachments (Trực tiếp sang class Attachment của DB)
                foreach (var mimePart in visitor.Attachments.OfType<MimePart>())
                {
                    // Tạo đối tượng Attachment (Entity)
                    var dbAttachment = new MailClient.Attachment();

                    dbAttachment.Name = mimePart.FileName ?? "unnamed";
                    dbAttachment.TypeMine = mimePart.ContentType.MimeType ?? "application/octet-stream"; // Lưu ý: TypeMine (theo class cũ của bạn)
                    dbAttachment.Size = (int)(mimePart.Content?.Stream?.Length ?? 0); // Ép kiểu long -> int
                    dbAttachment.IsDownload = 0; // Mặc định chưa tải

                    // Lưu "chìa khóa" MimePart vào thuộc tính tạm mới thêm
                    dbAttachment.OriginalMimePart = mimePart;

                    // Thêm vào danh sách tạm của Email
                    dbEmail.TempAttachments.Add(dbAttachment);
                }

                return dbEmail;
            });
        }

        public async Task SaveAttachmentAsync(MailClient.Attachment attachment, string savePath)
        {
            if (attachment.OriginalMimePart != null)
            {
                using (var stream = File.Create(savePath))
                {
                    await attachment.OriginalMimePart.Content.DecodeToAsync(stream);
                }
            }
        }

        // =========================================================================
        // PHẦN 2: TẠO GIAO DIỆN HTML (VIEW TEMPLATE - GMAIL STYLE)
        // =========================================================================

        public string GeneratePartialHtml(MailClient.Email email, string customAvatarUrl = null)
        {
            // Chuẩn bị dữ liệu
            string senderName = email.FromUser;
            string senderEmail = GetSenderEmail(email.From);
            string dateString = email.DateSent.ToString("HH:mm, dd/MM/yyyy");
            string initials = GetInitials(senderName);
            string avatarColor = GetColorFromString(senderName);
            string recipientsString = (email.To != null && email.To.Length > 0)
                              ? string.Join(", ", email.To)
                              : "me";

            // Xử lý Avatar HTML 
            string avatarHtml = !string.IsNullOrEmpty(customAvatarUrl)
                ? $@"<img class='avatar-img' src='{customAvatarUrl}' alt='AV' />"
                : $@"<div class='avatar-text' style='background-color: {avatarColor}'>{initials}</div>";

            // Xử lý Attachments từ List<Attachment> 
            StringBuilder attachmentsHtml = new StringBuilder();
            if (email.TempAttachments != null && email.TempAttachments.Count > 0)
            {
                attachmentsHtml.Append("<div class='attachments-area'>");
                attachmentsHtml.Append($"<div class='attachments-title'>{email.TempAttachments.Count} tệp đính kèm</div>");
                attachmentsHtml.Append("<div class='attachments-list'>");

                foreach (var att in email.TempAttachments)
                {
                    string size = FormatBytes(att.Size); // att.Size giờ là int
                    // Replace dấu ' trong tên file để tránh lỗi JS nếu tên file có ký tự đặc biệt
                    string safeName = att.Name.Replace("'", "\\'");
                    attachmentsHtml.Append($@"
                        <div class='attachment-card' title='{att.Name}' onclick=""sendDownloadRequest('{safeName}')"">
                            <div class='attachment-preview'>FILE</div>
                            <div class='attachment-footer'>
                                <div class='att-name'>{att.Name}</div>
                                <div class='att-size'>{size}</div>
                            </div>
                        </div>");
                }
                attachmentsHtml.Append("</div></div>");
            }

            string cleanBodyContent = ExtractBodyContent(email.BodyText); // Sử dụng BodyText

            string template = $@"             
                <div class='sender-header'>
                    <div class='avatar-container'>{avatarHtml}</div>
                    <div class='sender-info'>
                        <div class='sender-line-1'>
                            <span class='sender-name'>{System.Web.HttpUtility.HtmlEncode(senderName)}</span>
                            <span class='sender-email'>&lt;{System.Web.HttpUtility.HtmlEncode(senderEmail)}&gt;</span>
                        </div>
                        <div class='to-me'>tới {System.Web.HttpUtility.HtmlEncode(recipientsString)}</div>
                    </div>
                    <div class='email-date'>{dateString}</div>
                </div>

                <div class='email-body'>
                    {cleanBodyContent}
                </div>

                {attachmentsHtml}

                ";

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

        private string GetSenderName(string fullFromHeader)
        {
            if (string.IsNullOrEmpty(fullFromHeader)) return "Unknown";
            int index = fullFromHeader.IndexOf('<');
            if (index > 0) return fullFromHeader.Substring(0, index).Trim(' ', '"');
            return fullFromHeader;
        }

        private string GetSenderEmail(string fullFromHeader)
        {
            if (string.IsNullOrEmpty(fullFromHeader)) return "";
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
            var match = Regex.Match(html, @"<body[^>]*>(.*?)</body>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (match.Success)
            {
                content = match.Groups[1].Value;
            }

            // 2. Xóa các thẻ HTML bao quanh gây lỗi giao diện (nếu còn sót lại)
            content = Regex.Replace(content, @"<!DOCTYPE[^>]*>", "", RegexOptions.IgnoreCase);
            content = Regex.Replace(content, @"<html[^>]*>", "", RegexOptions.IgnoreCase);
            content = Regex.Replace(content, @"</html>", "", RegexOptions.IgnoreCase);
            content = Regex.Replace(content, @"<body[^>]*>", "", RegexOptions.IgnoreCase);
            content = Regex.Replace(content, @"</body>", "", RegexOptions.IgnoreCase);

            return content.Trim();
        }
    }
}