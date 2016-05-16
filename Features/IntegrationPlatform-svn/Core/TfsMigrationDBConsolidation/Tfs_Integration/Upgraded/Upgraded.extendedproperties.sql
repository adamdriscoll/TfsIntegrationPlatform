EXEC sp_addextendedproperty
    @name = 'FriendlyName',
    @value = 'TFS Integration Platform Database v2.9'
GO
/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script		
 Use SQLCMD syntax to include a file into the post-deployment script			
 Example:      :r .\filename.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/
EXEC sp_addextendedproperty 
    @name = 'ReferenceName',
    @value = '820D3636-9D14-4970-81CB-D0CD372F6D84'