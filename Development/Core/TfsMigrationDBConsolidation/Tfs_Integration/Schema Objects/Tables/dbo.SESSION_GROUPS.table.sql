CREATE TABLE [dbo].[SESSION_GROUPS]
(
	Id int NOT NULL identity(1,1), 
	GroupUniqueId uniqueidentifier NOT NULL,
	FriendlyName nvarchar(128) NOT NULL,
	State int NULL, -- 0: initialized; 1: running; 2: paused; 3: completed; 4. OneTimeCompleted (Can never be restarted)
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
