USE [WhiskWork]
GO
/****** Object:  Table [dbo].[WI_WorkItem]    Script Date: 03/06/2010 16:45:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[WI_WorkItem](
	[WI_Id] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[WI_Path] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[WI_Ordinal] [int] NULL,
	[WI_LastMoved] [datetime] NULL,
	[WI_Timestamp] [datetime] NULL,
	[WI_Status] [int] NOT NULL,
	[WI_ParentId] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[WI_ParentType] [int] NULL,
	[WI_Properties] [nvarchar](2048) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[WI_Classes] [nvarchar](1024) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_WI_WorkItem] PRIMARY KEY CLUSTERED 
(
	[WI_Id] ASC
)WITH (IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]


CREATE TABLE [dbo].[WS_WorkStep](
	[WS_Path] [nvarchar](2048) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[WS_ParentPath] [nvarchar](2048) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[WS_Ordinal] [int] NULL,
	[WS_Title] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[WS_Type] [int] NOT NULL,
	[WS_WorkItemClass] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[WS_WipLimit] [int] NULL
) ON [PRIMARY]
