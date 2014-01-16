BEGIN TRAN

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[WorkItems]') AND type in (N'U'))
BEGIN
	PRINT 'Table does not exist. Creating a new one.'

	CREATE TABLE [dbo].[WorkItems](
		[Id] [uniqueidentifier] NOT NULL,
		[Type] [nvarchar](max) NOT NULL,
		[Message] [nvarchar](max) NOT NULL,
		[Status] [int] NOT NULL,
		[Queue] [nvarchar](max) NOT NULL,
		[CreatedOn] [datetime] NOT NULL,
		[Version] [int] NOT NULL,
		[DispatchCount] int NOT NULL,
		[RetryOn] [datetime] NULL,
		[ParentId] [uniqueidentifier] NULL,
		[Log] [nvarchar](max) NULL

	CONSTRAINT [PK_dbo.WorkItems] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)
		WITH (PAD_INDEX  = OFF, 
		STATISTICS_NORECOMPUTE  = OFF, 
		IGNORE_DUP_KEY = OFF, 
		ALLOW_ROW_LOCKS  = ON, 
		ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	) ON [PRIMARY]

	ALTER TABLE [dbo].[WorkItems]  WITH CHECK ADD  CONSTRAINT [FK_dbo.WorkItems_dbo.WorkItems_ParentId] 
		FOREIGN KEY([ParentId])
		REFERENCES [dbo].[WorkItems] ([Id])

	ALTER TABLE [dbo].[WorkItems] CHECK CONSTRAINT [FK_dbo.WorkItems_dbo.WorkItems_ParentId]

	CREATE NONCLUSTERED INDEX [IX_RetryOn] ON [dbo].[WorkItems] 
	(
		[RetryOn] ASC
	)
	WITH (PAD_INDEX  = OFF, 
	STATISTICS_NORECOMPUTE  = OFF, 
	SORT_IN_TEMPDB = OFF, 
	IGNORE_DUP_KEY = OFF, 
	DROP_EXISTING = OFF, 
	ONLINE = OFF, 
	ALLOW_ROW_LOCKS  = ON, 
	ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]

END
ELSE 
BEGIN
	PRINT 'Updating the existing table.'	
END
COMMIT TRAN