drop database MailClient
Create Database MailClient
Go
use MailClient
Go
Create Table Account(	
	AccountID int Identity,				-- Khóa chính
	Email nvarchar(255) Unique,				-- Tên mail
	EncryptedPassword nvarchar(512),	-- Mật khẩu sau khi mã hóa
	AccountName nvarchar(100),			-- Tên tài khoản
	IncomingServer nvarchar(100),		-- tên miền server
	IncomingPort int,					-- Cổng nhận mail
	OutgoingServer nvarchar(100),		-- Server gửi mail
	OutgoingPort int,					-- Cổng gửi mail
	Constraint Account_PK Primary key (AccountID)
)
Create table Folder(
	FolderID int Identity,				-- Khóa chính 
	AccountID int,		--Khóa ngoại bảng Account
	FolderName nvarchar(100),			-- Tên thư mục
	TotalMail int default 0,			--Tổng số thư
	Constraint Folder_PK primary key (FolderID),
	Constraint Folder_FK Foreign key (AccountID) references Account(AccountID) on delete cascade
)
Create Table Email(
	ID bigint identity(1,1),		--Khóa chính
	AccountID int,
	FolderID int,			--Khóa ngoại bảng Folder
	SubjectEmail nvarchar(255),		--Subject email	
	FromAdd nvarchar(255),			
	ToAdd nvarchar(255),
	DateSent datetime,				--Ngày gửi
	DateReceived datetime,			--Ngày nhận
	BodyText nvarchar(Max),			--Nội dung
	IsRead bit default 0,			--Đánh dấu đã đọc
	IsFlag bit default 0,
	Constraint Email_PK primary key (ID),
	Constraint Email_FK_Folder foreign key (FolderID) references Folder(FolderID) on delete cascade,
	Constraint Email_FK_Account foreign key (AccountID) references Account(AccountID)
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
Select * from Account
select * from Folder
select * from Email
INSERT INTO Folder (AccountID, FolderName)
VALUES (5, N'Inbox');

INSERT INTO Folder (AccountID, FolderName)
VALUES (5, N'Sent');

INSERT INTO Folder (AccountID, FolderName)
VALUES (5, N'Spam');
INSERT INTO Folder (AccountID, FolderName)
VALUES (5, N'Draft');

INSERT INTO Email (AccountID, FolderID, SubjectEmail, FromAdd, ToAdd, DateSent, DateReceived, BodyText)
VALUES 
(5, 33, N'Welcome to the system', N'system@example.com', N'user5@example.com',
        '2025-01-10 08:30:00', '2025-01-10 08:30:05', N'Chào mừng bạn đến với hệ thống.');

INSERT INTO Email (AccountID, FolderID, SubjectEmail, FromAdd, ToAdd, DateSent, DateReceived, BodyText)
VALUES 
(5, 34, N'Monthly Report', N'report@example.com', N'user5@example.com',
        '2025-01-11 09:00:00', '2025-01-11 09:00:03', N'Báo cáo tháng đã được gửi.');

INSERT INTO Email (AccountID, FolderID, SubjectEmail, FromAdd, ToAdd, DateSent, DateReceived, BodyText)
VALUES 
(5, 35, N'Password Change Notification', N'security@example.com', N'user5@example.com',
        '2025-01-12 15:12:00', '2025-01-12 15:12:10', N'Mật khẩu tài khoản của bạn vừa được thay đổi.');

INSERT INTO Email (AccountID, FolderID, SubjectEmail, FromAdd, ToAdd, DateSent, DateReceived, BodyText)
VALUES 
(5, 33, N'Meeting Schedule', N'manager@example.com', N'user5@example.com',
        '2025-01-13 10:20:00', '2025-01-13 10:20:06', N'Cuộc họp sẽ diễn ra vào chiều nay.');

INSERT INTO Email (AccountID, FolderID, SubjectEmail, FromAdd, ToAdd, DateSent, DateReceived, BodyText)
VALUES 
(5, 34, N'Payment Received', N'billing@example.com', N'user5@example.com',
        '2025-01-14 13:45:00', '2025-01-14 13:45:04', N'Chúng tôi đã nhận được khoản thanh toán của bạn.');
