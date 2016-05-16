Team Foundation Server Integration Tools – March 2011 Release ReadMe

This document contains important and late breaking changes for the March 2011 release.


Known Issues and Workarounds:

1.	The migration Start button is disabled in tool after re-opening a bi-directional (2 way) synchronization session when the Integration Tools Windows Service is installed.  Workaround:   Close the Integration Tools application, and then restart the Windows Service.  After re-launching the Integration Tools, the buttons will be enabled.
2.	The TFS Integration Tools fail to run after upgrading from a previous version.  Workaround:  If you are upgrading the tools from a version prior to July 8th 2010, there is a versioning issue on the WinCredentials.dll file that causes the upgrade to leave mismatched files.  Uninstalling the prior version of the tools, and the installing the new version will workaround this issue.
3.	ClearCase selected history adapter conflict resolution may not work as expected.  Under some cases, after resolving a content conflict, additional conflicts may be generated.  This issue is currently under investigation and will be addressed in a future release.
4.	Migration of labels between version control servers is not supported using the Integration tools with any of our adapters.   We are reviewing this request, and it may be addressed in a future release.
5.	ClearCase views cannot be accessed by a Windows service, so if running a migration or sync involving ClearCase, the don't install the optional "TFS Integration Service" or edit MigrationToolServers.config and set "UseWindowsService" to "false".
6.	When configuring a ClearCase to TFS one-way migration, nothing will be migrated unless you check the checkbox labeled "Detect Changes in CC" in the "Connnect to ClearCase" dialog (which is unchecked by default).
7.	The installer may fail reporting the connection to the database failed when localhost was used as database server name. The workaround for this issue is to use machine name explicitly. 

This release contains fixes for the following bugs made since the November 2010 Release of the Integration Platform to Codeplex:

    ClearCase (forum post): User unable to configure ClearCase to TFS migration in Shell
    Null reference exception occurs in ClearCase adapter when CC ls command output is empty
    ClearQuest - TFS sync: Duplicate bugs are created in TFS
    ClearQuest - TFS sync: Editing a bug in TFS that originated as a CQ Defect causes Runtime Error with COMException on call to ClearQuest API
    ClearQuest - TFS sync: editing State field of a TFS Bug causes ArgumentNullException and stops sync if change would result in invalid CQ state transition
    ClearQuest - TFS sync: adding an attachment to either a CQ Defect or a TFS Bug does not work
    ClearQuest adapter fails to detect changes in ClearQuest when timezone is greater than UTC because it uses local time for the HighWaterMark
    ClearQuest - TFS sync: changing the only the State of a ClearQuest Defect does not cause it to be sync'd until another field is changed
    HighWaterMark data stored in SYNC_POINT table is wrong causing ServerDiff WIT to ignore many differences (and other problems)
    WIT sync fails for fields of type double with "decimal point is comma" setting
    ClearQuest - Migration is blocked by error on query for ClearQuest items to migrate if the CQ database server uses a Date format that is not the ISO standard format
    ClearQuest - ServerDiff Wit command does not work at all when the CQ filter string specifies a CQ stored query (very common)
    Key constraint violation occurs on the TFS_IntegrationPlatform DB when resolving a Namespace Conflict

This release also contains bug fixes included in the November 2010 Release of the Integration Platform to Codeplex that were not in the August 2010 Release to CodeGallery. 

Pre-requisites for using the Subversion adapter

    Runtime:
    The Interop.Subversion Assembly is a C++/CLI Assembly that is used to access the subversion repository.
    The subversion libraries are not statically linked or shipped with this release. Therefore you need to 
    deploy a Subversion binary distribution on your system before you can use this adapter. These binaries are 
    dynamically invoked (late binding) during the runtime of the project. Therefore you have to install the following
    package on your system first: http://sourceforge.net/projects/win32svn/. 
    1.6.13 is the latest version at the release time. 
    Please install the x32 bit package and not the x64 package.

    Compile Time:
    If you are building TFS Integration Platform from the source code on Codeplex, and need to build the Subversion Adapter, follow the steps outlined in 
    Adapters\Subversion\Interop.Subversion\ReadMe.txt.

    To build the Subversion Test Case Adapter (SubversionTCAdapter.csproj), you will need to install the SharpSVN library on your machine
    - You can download SharpSVN here: http://sharpsvn.open.collab.net/files/documents/180/2861/SSvn-1.6006.1373.zip
    - Extract the content of the zip file to "..\Binaries\External\SharpSVN"

Documentation Map:

* If you are looking for a documentation index, please open the index in:
  - Documentation\Readiness Package\Index.htm

* If you're looking to build the source code, please follow the process outlined in:
  - "Documentation\Readiness Package\Tooling Setup\TFS_Integration_Platform_-_Build_Instructions.docx"

* If you're looking to learn details about the platform, the core architecture, and 
the included TFS adapters, please check out:
  - "Documentation\Readiness Package\Guidance\TFS_Integration_Platform_-_Getting_Started.mht"
  - "Documentation\Readiness Package\Guidance\TFS_Integration_Platform_-_Architecture_Overview.mht"
  
* If you're looking to get started building an adapter, please refer to:
  - "Documentation\Readiness Package\Samples\TFS_Integration_Platform-Custom_Adapter_POC.mht"
  - "Documentation\Readiness Package\Samples\TFS_Integration_Platform-Wss_Adapter_Samples.mht"

  To build the Wix Installers, you will need to install Wix 3.5 on your machine.  You
  can download Wix 3.5 here:  http://wix.sourceforge.net/releases/3.5.2519.0/
