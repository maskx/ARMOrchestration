-- ============================================
-- {0} Schema
-- {1} Hub
-- {2} Deployment ServiceType
-- ============================================

IF(OBJECT_ID(N'[{0}].[{1}_WaitDependsOn]') IS NULL)
BEGIN
    CREATE TABLE [{0}].[{1}_WaitDependsOn] (
        [RootId] [nvarchar](50) NOT NULL,
        [DeploymentId] [nvarchar](50) NOT NULL,
        [InstanceId] [nvarchar](50) NOT NULL,
	    [ExecutionId] [nvarchar](50) NOT NULL,
        [EventName] [nvarchar](50) NOT NULL,
        [DependsOnName] [nvarchar](500) NOT NULL,
	    [CompletedTime] [datetime2](7) NULL,
	    [CreateTime] [datetime2](7) NULL
    )
END
GO

IF(OBJECT_ID(N'[{0}].[{1}_DeploymentOperations]') IS NULL)
BEGIN
    create table [{0}].[{1}_DeploymentOperations](
        [Id] [nvarchar](50) NOT NULL,
        [DeploymentId] [nvarchar](50) NOT NULL,
        [InstanceId] [nvarchar](50) NOT NULL,
	    [RootId] [nvarchar](50) NOT NULL,
        [CorrelationId] [nvarchar](50) NOT NULL,
	    [GroupId] [nvarchar](50) NOT NULL,
        [GroupType] [nvarchar](50) NOT NULL,
        [HierarchyId] [nvarchar](1024) NOT NULL,
        [ResourceId] [nvarchar](1024) NOT NULL,
        [Name] [nvarchar](1024) NOT NULL,
	    [Type] [nvarchar](200) NOT NULL,
	    [Stage] [int] NOT NULL,
	    [CreateTimeUtc] [datetime2](7) NOT NULL,
	    [UpdateTimeUtc] [datetime2](7) NOT NULL,
        [CreateByUserId] [nvarchar](50) NOT NULL,
        [LastRunUserId] [nvarchar](50) NOT NULL,
        [ApiVersion] [nvarchar](50) NOT NULL,
        [ExecutionId] [nvarchar](50)  NULL,
        [Comments] [nvarchar](256) NULL,
        [SubscriptionId] [nvarchar](50)  NULL,
        [ManagementGroupId] [nvarchar](50)  NULL,
        [ParentResourceId] [nvarchar](1024)  NULL,
        [Input] [nvarchar](max) NULL,
	    [Result] [nvarchar](max) NULL,
     CONSTRAINT [PK_{0}_{1}_DeploymentOperations] PRIMARY KEY CLUSTERED
    (
	    [Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
END
GO

IF(OBJECT_ID(N'[{0}].[{1}_DeploymentOperationHistory]') IS NULL)
BEGIN
    CREATE TABLE [{0}].[{1}_DeploymentOperationHistory](
	    [DeploymentOperationId] [nvarchar](50) NOT NULL,
	    [InstanceId] [nvarchar](50) NOT NULL,
	    [ExecutionId] [nvarchar](50) NOT NULL,
        [LastRunUserId] [nvarchar](50) NOT NULL,
        [UpdateTimeUtc] [datetime2](7) NOT NULL,
     CONSTRAINT [PK_{0}_{1}_DeploymentOperationHistory] PRIMARY KEY CLUSTERED 
    (
	    [DeploymentOPerationId] ASC,
	    [InstanceId] ASC,
	    [ExecutionId] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY]
END
GO

CREATE OR ALTER PROCEDURE [{0}].[{1}_InitRetry]
	@Id nvarchar(50),
	@CorrelationId nvarchar(50),
	@LastRunUserId nvarchar(50),
	@Input nvarchar(max)=null
AS
BEGIN
	declare @ParentResourceId nvarchar(1024)
	UPDATE [{0}].[{1}_DeploymentOperations]
	SET Stage=0,CorrelationId=@CorrelationId,LastRunUserId=@LastRunUserId,Input=ISNULL(@Input,Input),@ParentResourceId=ResourceId
	where Id=@Id and Stage=-1400

	if @ParentResourceId is not null
	begin
		declare @deploymentOperationId nvarchar(50)
		declare deployment_cursor CURSOR LOCAL  FORWARD_ONLY FOR SELECT Id FROM [{0}].[{1}_DeploymentOperations] where Stage=-1400  and ParentResourceId=@ParentResourceId;

		open deployment_cursor
		FETCH NEXT FROM deployment_cursor INTO @deploymentOPerationId
		while @@FETCH_STATUS=0
		BEGIN
			EXEC [dbo].[ARM_InitRetry] @deploymentOPerationId,@CorrelationId,@LastRunUserId
			FETCH NEXT FROM deployment_cursor INTO @deploymentOperationId
		END
		close deployment_cursor
		deallocate deployment_cursor
	end

	select case when @ParentResourceId  is NULL then 0 else 1 end  as Effected,Stage,CorrelationId,LastRunUserId from [{0}].[{1}_DeploymentOperations] where Id=@Id
END
GO

CREATE OR ALTER PROCEDURE [{0}].[{1}_PrepareRetry]
	@Id nvarchar(50),
	@NewInstanceId nvarchar(50),
	@NewExecutionId nvarchar(50),
	@LastRunUserId nvarchar(50),
	@Input nvarchar(max) =null
AS
BEGIN

	SET NOCOUNT ON;
	declare @InstanceId nvarchar(50)=null
	declare @ExecutionId nvarchar(50)=null
    declare @UpdateTimeUtc [datetime2](7)
    declare @RunUserId nvarchar(50)

	update [{0}].[{1}_DeploymentOperations]
	set InstanceId=@NewInstanceId,
		ExecutionId=@NewExecutionId,
		Stage=100,
		LastRunUserId=@LastRunUserId,
		Input=isnull(@Input,Input),
		@InstanceId=InstanceId, 
		@ExecutionId=ExecutionId,
		@UpdateTimeUtc=UpdateTimeUtc,
		@RunUserId=LastRunUserId
	where Id=@Id and Stage=0 
	if @@ROWCOUNT=1
	   BEGIN
	       insert into [{0}].[{1}_DeploymentOperationHistory] values(@Id,@InstanceId,@ExecutionId,@RunUserId,@UpdateTimeUtc)
	   END
	   select Stage from [{0}].[{1}_DeploymentOperations] where Id=@Id
END
GO

CREATE OR ALTER PROCEDURE [{0}].[{1}_CreateDeploymentOperation]
	@Id [nvarchar](50) ,
	@DeploymentId [nvarchar](50) ,
	@InstanceId [nvarchar](50) ,
	@RootId [nvarchar](50) ,
	@CorrelationId [nvarchar](50) ,
	@GroupId [nvarchar](50) ,
	@GroupType [nvarchar](50) ,
	@HierarchyId [nvarchar](1024),
	@ResourceId [nvarchar](1024) ,
	@Name [nvarchar](1024),
	@Type [nvarchar](200) ,
	@CreateByUserId [nvarchar](50),
	@ApiVersion [nvarchar](50),
	@ExecutionId [nvarchar](50),
	@Comments [nvarchar](256),
	@SubscriptionId [nvarchar](50),
	@ManagementGroupId [nvarchar](50),
	@ParentResourceId [nvarchar](1024),
	@Stage int,
	@Input [nvarchar](max) NULL
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @resId nvarchar(1024)
	SELECT @resId=ResourceId from [{0}].[{1}_DeploymentOperations] where CorrelationId=@CorrelationId and ParentResourceId is null
	IF @resId IS NULL OR @ParentResourceId IS NOT NULL
	BEGIN
		IF NOt EXISTS (select 0 from [{0}].[{1}_DeploymentOperations] where ResourceId=@ResourceId)
		BEGIN
			INSERT INTO [{0}].[{1}_DeploymentOperations]
			([ApiVersion],[Id],[InstanceId],[ExecutionId],[GroupId],[GroupType],[HierarchyId],[RootId],[DeploymentId],[CorrelationId],[ParentResourceId],[ResourceId],[Name],[Type],[Stage],[CreateTimeUtc],[UpdateTimeUtc],[SubscriptionId],[ManagementGroupId],[Input],[Comments],[CreateByUserId],[LastRunUserId])
			OUTPUT INSERTED.*
			VALUES
			(@ApiVersion,@Id,@InstanceId,@ExecutionId,@GroupId,@GroupType,@HierarchyId,@RootId,@DeploymentId,@CorrelationId,@ParentResourceId,@ResourceId,@Name,@Type,@Stage,GETUTCDATE(),GETUTCDATE(),@SubscriptionId,@ManagementGroupId,@Input,@Comments,@CreateByUserId,@CreateByUserId)
		END
		ELSE
		BEGIN
			SELECT * FROM [{0}].[{1}_DeploymentOperations] where ResourceId=@ResourceId
		END
	END
	ELSE
	BEGIN
		SELECT * FROM [{0}].[{1}_DeploymentOperations] where ResourceId=@ResourceId
	END
END
GO
