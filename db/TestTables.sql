USE [dapper_test]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Addresses](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ContactId] [int] NOT NULL,
	[Address1] [nvarchar](50) NOT NULL,
	[Address2] [nvarchar](50) NULL,
	[City] [nvarchar](50) NOT NULL,
	[County] [nvarchar](50) NULL,
	[PostCode] [nvarchar](10) NOT NULL,
 CONSTRAINT [PK_Addresses] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

CREATE TABLE [dbo].[Contacts](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_Contacts] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

ALTER TABLE [dbo].[Addresses]  WITH CHECK ADD  CONSTRAINT [FK_Addresses_Contacts] FOREIGN KEY([ContactId])
REFERENCES [dbo].[Contacts] ([Id])
GO

ALTER TABLE [dbo].[Addresses] CHECK CONSTRAINT [FK_Addresses_Contacts]
GO
