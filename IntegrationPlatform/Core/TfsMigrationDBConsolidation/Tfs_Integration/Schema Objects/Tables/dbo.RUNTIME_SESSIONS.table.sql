CREATE TABLE [dbo].[RUNTIME_SESSIONS]
(
	Id int identity(1,1) NOT NULL, 
	SessionUniqueId uniqueidentifier NOT NULL,
	LeftSourceId int NOT NULL,
	RightSourceId int NOT NULL,
	LeftHighWaterMarkLatest int NULL,
	RightHighWaterMarkLatest int NULL,
	LeftHighWaterMarkCurrent int NULL,
	RightHighWaterMarkCurrent int NULL,
	SessionGroupId int NOT NULL,
	ExecOrderInSessionGroup int,
	State int NULL, -- 0: initialized; 1: running; 2: paused; 3: completed;
	OrchestrationStatus int NULL -- Default = 0, 
                                 --
                                 -- primary states
                                 -- Started = 1,            
                                 -- Paused = 2,            
                                 -- StoppedSingleTrip = 3,            
                                 -- Stopped = 4,
                                 -- 
                                 -- intermittent states
                                 -- Starting = 5,
                                 -- Pausing = 6,
                                 -- StoppingSingleTrip = 7,
                                 -- Stopping = 8,
);
 