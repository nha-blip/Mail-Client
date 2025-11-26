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