Team Foundation Server Integration Tools – August 2010 Release ReadMe

This document contains important and late breaking changes for the August 2010 release.  




Known Issues and Workarounds:

1.	The migration Start button is disabled in tool after re-opening a bi-directional (2 way) synchronization session when the Integration Tools Windows Service is installed.  Workaround:   Close the Integration Tools application, and then restart the Windows Service.  After re-launching the Integration Tools, the buttons will be enabled.
2.	The TFS Integration Tools fail to run after upgrading from a previous version.  Workaround:  If you are upgrading the tools from a version prior to July 8th 2010, there is a versioning issue on the WinCredentials.dll file that causes the upgrade to leave mismatched files.  Uninstalling the prior version of the tools, and the installing the new version will workaround this issue.
3.	ClearCase selected history adapter conflict resolution may not work as expected.  Under some cases, after resolving a content conflict, additional conflicts may be generated.  This issue is currently under investigation and will be addressed in a future release.
4.	Migration of labels between version control servers is not supported using the Integration tools with any of our adapters.   We are reviewing this request, and it may be addressed in a future release.
5.	ClearCase views cannot be accessed by a Windows service, so if running a migration or sync involving ClearCase, the don't install the optional "TFS Integration Service" or edit MigrationToolServers.config and set "UseWindowsService" to "false".
6.	When configuring a ClearCase to TFS one-way migration, nothing will be migrated unless you check the checkbox labeled "Detect Changes in CC" in the "Connnect to ClearCase" dialog (which is unchecked by default).
7.  Installer may fail reporting connection to the database failed when localhost was used as database server name. The workaround for this issue is to use machine name explicitly. 



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
  can download Wix 3.5 here:  http://wix.sourceforge.net/releases/3.5.1616.0/

  * To build the Subversion Adapter, you will need to install the SharpSVN library on your machine
    - You can download SharpSVN here: http://sharpsvn.open.collab.net/files/documents/180/2861/SSvn-1.6006.1373.zip
	- Extract the content of the zip file to "..\Binaries\External\SharpSVN"