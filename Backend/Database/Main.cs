using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text;

namespace MailClient
{
    internal class MainProgram
    {
        static void Main()
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            Account account = new Account(new DatabaseHelper());
            // Các thông số IMAP/SMTP tiêu chuẩn cho Outlook
            account.AddAccount(
                "tranvanbinh@outlook.com.vn",
                "matkhaumahoa456",
                "Email công việc",
                "outlook.office365.com", // Máy chủ nhận thư
                993,
                587,
                "smtp.office365.com"    // Máy chủ gửi thư               
            );
            // Ví dụ cho một tài khoản email của công ty
            account.AddAccount(
                "support@congtycuaban.vn",
                "passtrong@#$",
                "Hỗ trợ khách hàng",
                "mail.congtycuaban.vn", // Thường có dạng mail.tên_miền
                993,
                465, 
                "mail.congtycuaban.vn" // Thường giống máy chủ nhận
                                    // Cổng gửi (SMTPS)
            );
            DataTable accounts = account.GetAllAccounts();
            foreach (DataRow row in accounts.Rows)
            {
                Console.WriteLine($"Email: {row["Email"]}, Tên tài khoản: {row["AccountName"]}");
            }
            Email email = new Email(new DatabaseHelper());
            email.AddEmail(
    FolderId: 5, // Giả sử ID 5 là thư mục "Quảng cáo"
    Subject: "Giảm giá 50% tất cả sản phẩm mùa thu!",
    From: "no-reply@cuahangthoitrang.com",
    To: "nguoidung@email.com",
    CC: "",
    BCC: "",
    Body: "Đừng bỏ lỡ cơ hội mua sắm lớn nhất trong năm! Nhấn vào đây để xem ngay.",
    sent: new DateTime(2025, 10, 20, 10, 30, 0), // Gửi từ hôm qua
    ReceivedAt: new DateTime(2025, 10, 20, 10, 31, 0), // Nhận từ hôm qua
    IsRead: true, // Đã đọc rồi
    IsFlag: false // Không đánh dấu
);
            email.AddEmail(
    FolderId: 1, // ID của thư mục "Hộp thư đến"
    Subject: "Xác nhận cuộc họp dự án Delta lúc 3 giờ chiều",
    From: "quanly.duan@congty.com",
    To: "nhom.phattrien@congty.com",
    CC: "giamdoc.kythuat@congty.com",
    BCC: "", // Không có BCC
    Body: "Chào đội ngũ, email này để xác nhận cuộc họp của chúng ta vào lúc 3 giờ chiều nay để thảo luận về các mốc thời gian của dự án Delta. Vui lòng chuẩn bị sẵn sàng.",
    sent: new DateTime(2025, 10, 21, 14, 0, 0), // Gửi lúc 2 giờ chiều
    ReceivedAt: DateTime.Now, // Nhận ngay bây giờ
    IsRead: false, // Chưa đọc
    IsFlag: true // Được đánh dấu quan trọng
);
            accounts = email.GetAllEmail();
            foreach (DataRow row in accounts.Rows)
            {
                Console.WriteLine($"Chủ đề: {row["SubjectEmail"]}, Từ: {row["FromAdd"]}, Đã đọc: {row["IsRead"]}");
            }

        }
    }
}
