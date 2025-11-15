Create Database MailClient
Go
use MailClient
Go
Create Table Account(
	ID int Identity(1,1),				--khóa chính
	Email nvarchar(255) Unique,				-- Tên mail
	EncryptedPassword nvarchar(512),	-- Mật khẩu sau khi mã hóa
	AccountName nvarchar(100),			-- Tên tài khoản
	IncomingServer nvarchar(100),		-- tên miền server
	IncomingPort int,					-- Cổng nhận mail
	OutgoingServer nvarchar(100),		-- Server gửi mail
	OutgoingPort int,					-- Cổng gửi mail
	Constraint Account_PK Primary key (ID)
)
Create table Folder(
	ID int identity(1,1),						--Khóa chính	
	AccountID int not null,						--Khóa ngoại bảng Account
	FolderName nvarchar(100),						-- Tên thư mục
	TotalMail int default 0,					--Tổng số thư
	Constraint Folder_PK primary key (ID),
	Constraint Folder_FK Foreign key (AccountID) references Account(ID) on delete cascade
)
Create Table Email(
	ID bigint identity(1,1),		--Khóa chính
	FolderID int not null,			--Khóa ngoại bảng Folder
	SubjectEmail nvarchar(255),		--Subject email	
	FromAdd nvarchar(255),			
	ToAdd nvarchar(255),
	DateSent datetime,				--Ngày gửi
	DateReceived datetime,			--Ngày nhận
	BodyText nvarchar(Max),			--Nội dung
	IsRead bit default 0,			--Đánh dấu đã đọc
	IsFlag bit default 0,
	Constraint Email_PK primary key (ID),
	Constraint Email_FK foreign key (FolderID) references Folder(ID) on delete cascade,
)
create table Attachment(
	ID int identity (1,1),			--Khóa chính
	EmailID bigint not null,		--Khóa ngoại bảng Email
	NameFile nvarchar(255),			--Tên file
	TypeMime nvarchar(100),			--Kiểu file (pdf,word,..)
	Size bigint,					--Kích thước
	Downloaded bit default 0,		--Đã tải
	Isflag bit default 0,
	Constraint Attachment_PK Primary key (ID),
	Constraint Attachment_FK foreign key (EmailID) references Email(ID) on delete cascade
)
select *
from Email
Drop table Folder
SELECT * FROM Folder;

INSERT INTO Folder (AccountID, FolderName, TotalMail)
VALUES
(1, 'Inbox', 0),
(1, 'Sent', 0),
(1, 'Trash', 0),
(2, 'Inbox', 0),
(2, 'Archive', 0);

INSERT INTO Email (FolderID, SubjectEmail, FromAdd, ToAdd, DateSent, DateReceived, BodyText, IsRead, IsFlag)
VALUES 
(9, 'Meeting Reminder', 'boss@example.com', 'employee1@example.com;employee2@example.com', '2025-11-11 08:30', '2025-11-11 08:31', 'Please attend the meeting at 9 AM.', 0, 0),

(9, 'Project Update', 'manager@example.com', 'team@example.com', '2025-11-10 10:00', '2025-11-10 10:01', 'Project status update attached.', 1, 0),

(9, 'Invoice November', 'finance@example.com', 'client@example.com', '2025-11-09 09:00', '2025-11-09 09:05', 'Please find attached invoice for November.', 0, 1),

(9, 'Party Invitation', 'hr@example.com', 'staff@example.com', '2025-11-08 15:00', '2025-11-08 15:02', 'Join us for the company party this Friday.', 1, 0),

(9, 'Weekly Report', 'teamlead@example.com', 'manager@example.com', '2025-11-07 18:00', '2025-11-07 18:05', 'Attached is the weekly report.', 0, 0);
