REM Generate sql script for installer

"C:\Program Files (x86)\Microsoft Visual Studio 9.0\DBPro\sqlspp.exe" /i:"..\Binaries\Debug\SQL\Tfs_IntegrationPlatform.sql" /o:"..\Binaries\Debug\SQL\output.sql"
copy ..\Binaries\Debug\SQL\output.sql ..\Setup\Installation\Tfs_IntegrationPlatform.sql
pause
