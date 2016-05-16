MSBuild ..\..\MigrationTools.sln


MSBuild /target:Deploy "..\..\Core\TfsMigrationDBConsolidation\Tfs_Integration\Tfs_IntegrationPlatform.dbproj"


MSBuild ..\MigrationTest\MigrationTest.sln

xcopy /yed ..\..\Binaries\Debug\bin\i386\Plugins\*Adapter.* ..\MigrationTest\E2ETest\Plugins


notepad.exe "..\Doc\EarlyIntegrationTests\FirstMigrationConfig.xml"
