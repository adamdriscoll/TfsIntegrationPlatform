========================================================================
    DYNAMIC LINK LIBRARY : Interop.Subversion Project Overview
========================================================================

Runtime:
The Interop.Subversion Assembly is a C++/CLI Assembly that is used to access the subversion repository.
The subversion libraries are not statically linked or shipped with this release. Therefore you need to 
deploy an svn binary distribution on your system before you can use this adapter. These binaries are 
dynaically invoked (late binding) during the runtime of the project. Therefore you have to install the following
package on your system first: http://sourceforge.net/projects/win32svn/. Please install the x86 bit package and not the x64
package.

Compile Time:
In order to compile the solution you need to install the Subversion and the apr header files on your system. 
Therefore you have to download the following packages:

- Subversion 1.6.x Build.  You can find the latest build here:  http://subversion.apache.org/
For example:  http://subversion.tigris.org/downloads/subversion-1.6.13.zip
Note that there may be later versions available now.

- Apache Portable Runtime (APR) 1.3.x build or later.  You can find the latest build here: http://apr.apache.org/
For example: http://ftp.wayne.edu/apache//apr/apr-1.4.2-win32-src.zip
Note that there may be later versions available now.

Create a folder named ExternalSources in the same folder as the Visual Studio solution (MigrationTools.sln).
(SolutionFolder)\ExternalSources

Then extract the content of the files to the following folders:
(SolutionFolder)\ExternalSources\Subversion
(SolutionFolder)\ExternalSources\Apr

Compile the APR Library. We just have to compile it because the configure process is going to create a header file that we need.
nmake is available from the Visual Studio Command Prompt.

> cd (SolutionFolder)\ExternalSources\Apr
> nmake -f Makefile.win buildall

Development Advice:
It is recommanded to use the dependency walker during the development process. The dependency walker can resolve the symbols that 
are stored in the binary. These names are needed for the GetProcAdress Method. 
