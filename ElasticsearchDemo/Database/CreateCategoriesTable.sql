USE ProductDB;
GO

CREATE TABLE Categories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    CreatedDate DATETIME NOT NULL,
    UpdatedDate DATETIME NULL
);
GO

-- Products tablosundaki CategoryId'ye foreign key ekliyoruz
ALTER TABLE Products
ADD CONSTRAINT FK_Products_Categories
FOREIGN KEY (CategoryId) REFERENCES Categories(Id);
GO 