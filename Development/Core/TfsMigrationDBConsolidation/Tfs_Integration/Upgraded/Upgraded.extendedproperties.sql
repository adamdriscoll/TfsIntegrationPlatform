EXEC sp_addextendedproperty
    @name = 'FriendlyName',
    @value = 'TFS Integration Platform Database v2.10'
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
    @value = 'FC4AE969-B5A7-4A03-ADF1-FDDCAFB6FFD0'