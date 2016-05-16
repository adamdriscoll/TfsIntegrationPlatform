REM copy plugins to test directory
xcopy /yedr ..\..\..\Binaries\Debug\bin\i386\Plugins\*.dll ..\..\MigrationTest\E2ETest\Plugins

REM create workspace root directory
mkdir C:\TfsMigrtData

REM create a global configuration file
mkdir "%ALLUSERSPROFILE%\Microsoft\Team Foundation\Migration Tools"
erase "%ALLUSERSPROFILE%\Microsoft\Team Foundation\Migration Tools\MigrationToolServers.config"
xcopy /yedr ..\..\..\Tools\MigrationConsole\MigrationToolConfig\MigrationToolServers.config "%ALLUSERSPROFILE%\Microsoft\Team Foundation\Migration Tools"
