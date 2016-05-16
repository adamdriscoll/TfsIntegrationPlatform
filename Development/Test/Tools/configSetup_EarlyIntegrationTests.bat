mkdir C:\TfsMigrtData
mkdir "%ALLUSERSPROFILE%\Microsoft\Team Foundation\Migration Tools"
erase "%ALLUSERSPROFILE%\Microsoft\Team Foundation\Migration Tools\MigrationToolServers.config"
xcopy /yedr ..\..\Test\MigrationTest\MigrationConsole\MigrationToolConfig\MigrationToolServers.config "%ALLUSERSPROFILE%\Microsoft\Team Foundation\Migration Tools"
notepad.exe "%ALLUSERSPROFILE%\Microsoft\Team Foundation\Migration Tools\MigrationToolServers.config"