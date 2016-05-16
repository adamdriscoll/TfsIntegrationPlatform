@ECHO OFF
SET ReleaseCodeFolder=%CD%

ECHO Create the expected folder structure
CD ..
ECHO MKDIR TestEnv
MKDIR TestEnv

CD %ReleaseCodeFolder%

ECHO Copy MigrationTestEnvironment.xml
COPY Test\MigrationTest\MigrationTestLibrary\TestEnvironment\MigrationTestEnvironmentTemplate.xml ..\TestEnv\MigrationTestEnvironment.xml

ECHO Done!
