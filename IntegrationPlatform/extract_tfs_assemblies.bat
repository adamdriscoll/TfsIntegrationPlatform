@ECHO OFF
SET ReleaseCodeFolder=%CD%

ECHO Create the expected folder structure
CD ..
ECHO MKDIR Binaries\Internal\TFS2008
MKDIR Binaries\Internal\TFS2008
ECHO MKDIR Binaries\Internal\TFS2010
MKDIR Binaries\Internal\TFS2010
ECHO MKDIR Binaries\Internal\TFS11
MKDIR Binaries\Internal\TFS11

ECHO Probing GAC to pick up TFS private assemblies
%SYSTEMDRIVE%
CD %SYSTEMROOT%\ASSEMBLY
ECHO Probing Microsoft.TeamFoundation.Client.dll
DIR Microsoft.TeamFoundation.Client.dll /S /B             			> %ReleaseCodeFolder%\output1.txt
ECHO Probing Microsoft.TeamFoundation.Common.dll 
DIR Microsoft.TeamFoundation.Common.dll /S /B             			>> %ReleaseCodeFolder%\output1.txt
ECHO Probing Microsoft.TeamFoundation.Common.Library.dll 
DIR Microsoft.TeamFoundation.Common.Library.dll /S /B     			>> %ReleaseCodeFolder%\output1.txt
ECHO Probing Microsoft.TeamFoundation.dll 
DIR Microsoft.TeamFoundation.dll /S /B                    			>> %ReleaseCodeFolder%\output1.txt
ECHO Probing Microsoft.TeamFoundation.VersionControl.Client.dll 
DIR Microsoft.TeamFoundation.VersionControl.Client.dll /S /B 		>> %ReleaseCodeFolder%\output1.txt
ECHO Probing Microsoft.TeamFoundation.VersionControl.Common.dll 
DIR Microsoft.TeamFoundation.VersionControl.Common.dll /S /B 		>> %ReleaseCodeFolder%\output1.txt
ECHO Probing Microsoft.TeamFoundation.WorkItemTracking.Client.dll 
DIR Microsoft.TeamFoundation.WorkItemTracking.Client.dll /S /B 		>> %ReleaseCodeFolder%\output1.txt
ECHO Probing Microsoft.TeamFoundation.WorkItemTracking.Controls.dll 
DIR Microsoft.TeamFoundation.WorkItemTracking.Controls.dll /S /B 	>> %ReleaseCodeFolder%\output1.txt
ECHO Probing Microsoft.TeamFoundation.WorkItemTracking.Proxy.dll 
DIR Microsoft.TeamFoundation.WorkItemTracking.Proxy.dll /S /B 		>> %ReleaseCodeFolder%\output1.txt
ECHO Probing Microsoft.TeamFoundation.WorkItemTracking.Common.dll 
DIR Microsoft.TeamFoundation.WorkItemTracking.Common.dll /S /B 		>> %ReleaseCodeFolder%\output1.txt


CD %ReleaseCodeFolder%

ECHO Copy assemblies
For /f "tokens=* delims=" %%a in (%ReleaseCodeFolder%\output1.txt) DO (

	echo %%a > %ReleaseCodeFolder%\output2.txt

	FOR /f "tokens=7* delims=\_" %%G in (%ReleaseCodeFolder%\output2.txt) DO (

		IF "%%G" == "9.0.0.0" COPY "%%a" "%ReleaseCodeFolder%\..\Binaries\Internal\TFS2008"
		IF "%%G" == "10.0.0.0" COPY "%%a" "%ReleaseCodeFolder%\..\Binaries\Internal\TFS2010"
		IF "%%G" == "11.0.0.0" COPY "%%a" "%ReleaseCodeFolder%\..\Binaries\Internal\TFS11"
	)
)

ECHO Clean up
DEL %ReleaseCodeFolder%\output1.txt
DEL %ReleaseCodeFolder%\output2.txt

ECHO Done!

