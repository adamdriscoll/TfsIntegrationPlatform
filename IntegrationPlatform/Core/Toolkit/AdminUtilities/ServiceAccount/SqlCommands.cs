// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    static class SqlCommands
    {
        public const string IsAccountInTFSIPEXECRole =
@"
IF is_rolemember('TFSIPEXEC', @account) <> 1
BEGIN
    --not in the role
    IF is_rolemember('db_owner', @account) <> 1
    BEGIN
        SELECT 0; --not db owner either: return false
        RETURN; 
    END
    ELSE
    BEGIN
        SELECT 1; --is db owner: return true
        RETURN;
    END
END
ELSE
BEGIN
    SELECT 1;
    RETURN; --in the role: return true
END
";

        public const string AddAccountToTFSIPEXECRole =
    @"
DECLARE @return INT

IF is_rolemember('db_owner', @account) <> 1
BEGIN
    EXEC @return = sp_addrolemember 'TFSIPEXEC', @account

    IF (@return <> 0)
    BEGIN 
        SELECT 1;
        RETURN; --error 
    END    
    
    SELECT 0; --success
    RETURN; 
END
ELSE
BEGIN
    -- skip adding db owner to the role
    SELECT 2;
    RETURN; --noop
END
";

        public const string CreateTFSIPEXECRole =
@"
DECLARE @status INT

IF NOT EXISTS (
        SELECT  *
        FROM    sys.database_principals
        WHERE   name = 'TFSIPEXEC'
                AND type = 'R')
BEGIN
    CREATE ROLE [TFSIPEXEC]
    SET @status = @@ERROR
    IF (@status <> 0)
    BEGIN
        SELECT 1; 
        RETURN; --error
    END
END            

IF DB_ID() = 1
BEGIN
    -- do nothing with master DB
    SELECT 2;
    RETURN; --noop
END
ELSE
BEGIN
    -- Ensure that the TFSIPEXEC has permissions to read the extended properties on the DB
    GRANT VIEW DEFINITION TO TFSIPEXEC
    -- We need to grant much less permissions on master
    GRANT CONTROL TO TFSIPEXEC
    
    -- Obsolete: we try to grant less permissions than db owner has
    -- EXEC sp_addrolemember @rolename = 'db_owner', @membername = 'TFSIPEXEC'

    -- Allow TFSIPEXEC role members to write data to the database
    -- EXEC sp_addrolemember @rolename = 'db_ddladmin', @membername = 'TFSIPEXEC'
    EXEC @status = sp_addrolemember @rolename = 'db_datawriter', @membername = 'TFSIPEXEC'
    IF (@status <> 0)
    BEGIN 
        SELECT 1;
        RETURN; --error 
    END

    -- Allow @status = TFSIPEXEC role members to read data form the database
    EXEC sp_addrolemember @rolename = 'db_datareader', @membername = 'TFSIPEXEC'
    IF (@status <> 0)
    BEGIN 
        SELECT 1;
        RETURN; --error 
    END

    -- Allow TFSIPEXEC to execute all the sproc in the database
    GRANT EXECUTE TO [TFSIPEXEC]
    SET @status = @@ERROR
    IF (@status <> 0)
    BEGIN 
        SELECT 1;
        RETURN; --error 
    END  
END

SELECT 0;
RETURN;
";

        public const string CreateWindowsLogin =
@"
-- Creates a Windows authentication login if does not exist
-- Parameter:
--    @loginName - login to create

IF NOT EXISTS(
    SELECT  *
    FROM    sys.syslogins l
    WHERE   l.loginname = @loginName
)
BEGIN
    DECLARE @stmt NVARCHAR(1000)
    SET @stmt = 'CREATE LOGIN ' + QUOTENAME(@loginName) + ' FROM WINDOWS'
    EXEC sp_executesql @stmt
END

-- query user for the specified login
DECLARE @statement          NVARCHAR(4000)
DECLARE @return             INT
DECLARE @accountsResult     INT
DECLARE @userName           NVARCHAR(4000)
SET @accountsResult = 1

SELECT  @userName = dp.name
FROM    sys.database_principals dp
JOIN    sys.syslogins l
ON      dp.sid = l.sid
WHERE   dp.type = 'U'
        AND l.name =  @loginName

-- Create a user for this login if it does not exist
IF (@userName IS NULL)
BEGIN
    SET @userName = @loginName
    -- Before creating a user, let us check that user with the same name does not exist.
    -- if it exists, we need to drop it provided that it is not mapped to other logins.
    -- We already verified that login is not mapped to any users in this database.
    -- If [domain-name\user-name] user exists than we are in scenario when someone created a local user, created SQL login for that windows user
    -- and mapped a login to the database user.
    -- After that customer deleted an NT local user, dropped login. 
    -- Later customer recreated NT local user and login. SID of login changed but SID of the database use did not.
        
    IF ( EXISTS(SELECT  *
                FROM    sys.database_principals p
                LEFT JOIN sys.syslogins l
                ON      p.sid = l.sid
                WHERE   p.name = @userName
                        AND l.sid IS NULL) )
    BEGIN
        -- We cannot drop user if it owns any roles.
        -- Transfer role ownership to the dbo user.
        DECLARE @owningRole NVARCHAR(4000)
        DECLARE owningRolesCursor CURSOR FOR
        SELECT  name
        FROM    sys.database_principals dp
        WHERE   dp.owning_principal_id = USER_ID(@userName)

        OPEN owningRolesCursor

        FETCH NEXT FROM owningRolesCursor
        INTO @owningRole

        WHILE @@FETCH_STATUS = 0
        BEGIN
            DECLARE @stmt1 NVARCHAR(1000)
            PRINT 'Transfering ownership of ' + @owningRole + ' role to dbo.' 
            SET @stmt1 = 'ALTER AUTHORIZATION ON ROLE::' + QUOTENAME(@owningRole) + ' TO dbo'
            EXEC(@stmt1)
            SET @return = @@ERROR
            IF (@return <> 0)
            BEGIN 
                CLOSE owningRolesCursor
                DEALLOCATE owningRolesCursor

                SELECT 1;
                RETURN; --error 
            END

            FETCH NEXT FROM owningRolesCursor
            INTO    @owningRole
        END
        CLOSE owningRolesCursor
        DEALLOCATE owningRolesCursor
        
        PRINT 'Dropping user: ' + @userName
        SET @statement = N'DROP USER ' + QUOTENAME(@userName)
        EXEC(@statement)
        SET @return = @@ERROR
        IF (@return <> 0)
        BEGIN 
            SELECT 1;
            RETURN; --error 
        END            
    END     
        
    PRINT 'Creating user: ' + @loginName 
    SET @statement = 'CREATE USER ' + QUOTENAME(@userName) + ' FOR LOGIN ' + QUOTENAME(@loginName)
    EXEC(@statement)
    SET @return = @@ERROR
    IF (@return <> 0)
    BEGIN 
        SELECT 1;
        RETURN; --error 
    END
END

SELECT 0;
RETURN;
";

        public const string RemoveAccountFromTFSIPEXECRole =
@"
DECLARE @return INT

--
-- if not in role, no op
--
print 'checking if already in role';
IF NOT EXISTS 
(
    SELECT  *
    FROM    sys.database_principals rolep
    JOIN    sys.database_role_members drm
    ON      drm.role_principal_id = rolep.principal_id
    JOIN    sys.database_principals memberp
    ON      memberp.principal_id = drm.member_principal_id
    WHERE   rolep.type = 'R' AND rolep.name = 'TFSIPEXEC'
            AND memberp.name = @account
) 
BEGIN    
    print @account + ' is not in the role. noop';
    SELECT 2; -- noop (in role already)
    RETURN;
END
    
-- remove the account from the role
EXEC @return = sp_droprolemember 'TFSIPEXEC', @account
IF (@return <> 0)
BEGIN 
    SELECT 1;
    RETURN; --error 
END    
    
SELECT 0; --success
RETURN; 
";
    }
}
