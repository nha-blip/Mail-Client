using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MimeKit;
using MimeKit.Text;

namespace MailClient.Core.Parsing
{
    public class HtmlPreviewVisitor : MimeVisitor
    {
        readonly List<MultipartRelated> _stack = new List<MultipartRelated>();
        readonly List<MimeEntity> _attachments = new List<MimeEntity>();
        string _htmlBody;

        /// <summary>
        /// Danh sách các file đính kèm thực sự (không phải ảnh inline)
        /// </summary>
        public IList<MimeEntity> Attachments
        {
            get { return _attachments; }
        }

        /// <summary>
        /// Chuỗi HTML "sạch" (đã xử lý cid:) để đưa cho WebView2
        /// </summary>
        public string HtmlBody
        {
            get { return _htmlBody ?? string.Empty; }
        }

        // Tìm phần body phù hợp nhất để hiển thị
        protected override void VisitMultipartAlternative(MultipartAlternative alternative)
        {
            // Đi ngược từ phiên bản "tốt nhất" (ví dụ: html) xuống
            for (int i = alternative.Count - 1; i >= 0; i--)
            {
                var body = alternative[i] as TextPart;
                if (body != null && body.IsHtml)
                {
                    body.Accept(this);
                    return; // Tìm thấy HTML, dừng lại
                }
            }

            // Nếu không có HTML, tìm text
            for (int i = alternative.Count - 1; i >= 0; i--)
            {
                var body = alternative[i] as TextPart;
                if (body != null && body.IsPlain)
                {
                    body.Accept(this);
                    return; // Tìm thấy text, dừng lại
                }
            }
        }

        // Theo dõi các phần "related" để tìm ảnh cid:
        protected override void VisitMultipartRelated(MultipartRelated related)
        {
            var root = related.Root;

            _stack.Add(related);
            root.Accept(this);
            _stack.RemoveAt(_stack.Count - 1);
        }

        // Xử lý các phần text (HTML hoặc Plain)
        // Dán code này vào HtmlPreviewVisitor.cs (thay thế hàm VisitTextPart cũ)

        protected override void VisitTextPart(TextPart entity)
        {
            if (entity.IsHtml)
            {
                // Logic xử lý HTML (đã đúng)
                _htmlBody = TransformHtml(entity.Text);
            }
            else if (entity.IsPlain)
            {
                // *** CODE ĐÃ SỬA LỖI UTF-8 ***

                // 1. Chỉ encode các ký tự HTML nguy hiểm
                string encoded = entity.Text.Replace("&", "&amp;")
                                            .Replace("<", "&lt;")
                                            .Replace(">", "&gt;");

                // 2. Chuyển đổi xuống dòng (xử lý cả \r\n và \n)
                _htmlBody = encoded.Replace("\r\n", "<br>\n").Replace("\n", "<br>\n");
            }
        }

        // Thu thập các file đính kèm "thực sự"
        protected override void VisitMimePart(MimePart entity)
        {
            // Chỉ thêm nếu nó được đánh dấu là "attachment"
            if (entity.IsAttachment)
                _attachments.Add(entity);
        }

        // Hàm "ăn tiền" - viết lại HTML để xử lý cid:
        string TransformHtml(string html)
        {
            var tokenizer = new HtmlTokenizer(new StringReader(html));
            var output = new StringWriter();

            while (tokenizer.ReadNextToken(out var token))
            {
                if (token.Kind == HtmlTokenKind.Tag)
                {
                    var tag = (HtmlTagToken)token;
                    if (tag.Id == HtmlTagId.Image && !tag.IsEndTag)
                    {
                        // Đây là thẻ <img>, tìm thuộc tính "src"
                        bool srcFound = false;
                        foreach (var attr in tag.Attributes)
                        {
                            if (attr.Id == HtmlAttributeId.Src && attr.Value != null)
                            {
                                srcFound = true;
                                if (attr.Value.StartsWith("cid:", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Nó là ảnh cid:, tìm ảnh đó
                                    var mimePart = FindImageByCid(attr.Value);
                                    if (mimePart != null)
                                    {
                                        // Đã tìm thấy, chuyển ảnh sang Data URI (Base64)
                                        string dataUri = GetDataUri(mimePart);

                                        // *** ĐÂY LÀ PHẦN ĐÃ SỬA ***
                                        // Viết lại thẻ <img> bằng tay
                                        WriteModifiedImgTag(output, tag, dataUri);
                                    }
                                    else
                                    {
                                        // Không tìm thấy ảnh, giữ nguyên thẻ cũ
                                        token.WriteTo(output);
                                    }
                                }
                                else
                                {
                                    // src không phải cid:, giữ nguyên thẻ cũ
                                    token.WriteTo(output);
                                }
                                goto next_token;
                            }
                        }

                        if (!srcFound)
                        {
                            // Thẻ <img> không có src, giữ nguyên
                            token.WriteTo(output);
                        }
                    }
                    else
                    {
                        // Không phải thẻ <img>, giữ nguyên
                        token.WriteTo(output);
                    }
                }
                else
                {
                    // Không phải thẻ (ví dụ: text, comment), giữ nguyên
                    token.WriteTo(output);
                }

            next_token:
                continue;
            }

            return output.ToString();
        }

        // Hàm trợ giúp mới để viết lại thẻ <img>
        void WriteModifiedImgTag(TextWriter output, HtmlTagToken tag, string newDataUri)
        {
            output.Write($"<{tag.Name}"); // Viết "<img"

            foreach (var attribute in tag.Attributes)
            {
                output.Write(" ");
                if (attribute.Id == HtmlAttributeId.Src)
                {
                    // Đây là thuộc tính src, hãy viết giá trị dataUri mới
                    output.Write("src=\"");
                    output.Write(newDataUri); 
                    output.Write("\"");
                }
                else
                {
                    // Viết các thuộc tính khác (alt, style,...) như cũ
                    output.Write(attribute.Name);
                    output.Write("=\"");
                    HtmlUtils.HtmlAttributeEncode(output, attribute.Value);
                    output.Write("\"");
                }
            }

            if (tag.IsEmptyElement)
                output.Write(" /");
            output.Write(">");
        }

        // Tìm MimePart (ảnh) tương ứng với cid:
        // Dán code này vào file HtmlPreviewVisitor.cs
        // thay thế cho hàm FindImageByCid CŨ

        MimePart FindImageByCid(string cid)
        {
            var uri = new Uri(cid);
            string contentId = uri.AbsolutePath; // Lấy ID (ví dụ: "ii_mh0cexgj1")

            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                var related = _stack[i];

                // Dùng hàm GetEntity(string contentId) có sẵn của MimeKit
                // Hàm này đủ thông minh để xử lý ContentId có dấu <> hoặc không
                var entity = related.FirstOrDefault(p => p.ContentId == contentId);

                if (entity is MimePart part)
                    return part; // Tìm thấy!
            }

            return null; // Không tìm thấy
        }

        // Chuyển MimePart (ảnh) thành chuỗi Data URI (Base64)
        string GetDataUri(MimePart image)
        {
            using (var memory = new MemoryStream())
            {
                image.Content.DecodeTo(memory);
                var buffer = memory.ToArray();
                var contentType = image.ContentType.MimeType;
                return $"data:{contentType};base64,{Convert.ToBase64String(buffer)}";
            }
        }
    }
}
