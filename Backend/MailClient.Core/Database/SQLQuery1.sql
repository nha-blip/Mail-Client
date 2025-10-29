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
	UseSSL bit Default 1,				-- có bảo mật
	LastSyncTime Datetime,				-- ngày đồng bộ
	Constraint Account_PK Primary key (ID)
)
Create table Folder(
	ID int identity(1,1),						--Khóa chính	
	AccountID int not null,						--Khóa ngoại bảng Account
	FdName nvarchar(100),						-- Tên thư mục
	SyncState varchar(50) default 'Pending',	--Trạng thái đồng bộ
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
	ccAdd nvarchar(Max),
	bcc nvarchar(Max),
	DateSent datetime,				--Ngày gửi
	DateReceived datetime,			--Ngày nhận
	BodyText nvarchar(Max),			--Nội dung
	IsRead bit default 0,			--Đánh dấu đã đọc
	IsFlag bit default 0,			--Thư được đính dấu
	HasAttachment bit,				--Có tệp đính kèm ko
	MessageID varchar(255),			--Định danh toàn cầu
	Constraint Email_PK primary key (ID),
	Constraint Email_FK foreign key (FolderID) references Folder(ID) on delete cascade,
)
create table Attachment(
	ID int identity (1,1),			--Khóa chính
	EmailID bigint not null,		--Khóa ngoại bảng Email
	NameFile nvarchar(255),			--Tên file
	TypeMime nvarchar(100),			--Kiểu file (pdf,word,..)
	Size bigint,					--Kích thước
	PathFile nvarchar(400),			--Đường dẫn
	Downloaded bit default 0,		--Đã tải
	Constraint Attachment_PK Primary key (ID),
	Constraint Attachment_FK foreign key (EmailID) references Email(ID) on delete cascade
)