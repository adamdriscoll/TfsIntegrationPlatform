CREATE TABLE [dbo].RUNTIME_ORCHESTRATION_COMMAND
(
	Id int identity NOT NULL PRIMARY KEY,
	SessionGroupId int NOT NULL,
	Command int NOT NULL, -- DEFAULT = 0, 
						  -- START = 1,
						  -- PAUSE = 2,
						  -- RESUME = 3,
						  -- STOP_CURRENT_TRIP = 4,
						  -- START_NEW_TRIP = 5,
						  -- STOP = 6,
						  -- FINISH = 7, -- one-shot migration session only
						  
	Status int NOT NULL   -- New = 0,
						  -- Processing = 1,
						  -- Processed = 2,
);