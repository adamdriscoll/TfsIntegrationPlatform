#pragma once

#include "Library.h"
#include "DynamicInvocationAttribute.h"

using namespace System::IO;
using namespace System::Collections::Generic;
using namespace System::Threading;
using namespace System::Reflection;

namespace Microsoft
{
	namespace TeamFoundation
	{
		namespace Migration
		{
			namespace SubversionAdapter
			{
				namespace Interop
				{
					namespace Subversion
					{
						namespace DynamicInvocation
						{
							private ref class LibraryLoader
							{
							private:
								//This dictionary has a reference to all items that have been loaded
								initonly Dictionary<String^, Library^>^ m_loadedLibs;

								//static variable for the singleton pattern
								static LibraryLoader^ m_loader = gcnew LibraryLoader();

								String^ m_SubversionInstallationDirectory;

								/// <summary>
								/// Constructor of the LibraryLoader Class. The constructor is public. Clients have to use the factory method to retrieve a reference
								/// </summary>
								LibraryLoader();

								/// <summary>
								/// Creates a canonical path of the given name to make it compareable
								/// </summary>
								String^ GetNormalizedLibraryString(String^ name);

								/// <summary>
								/// This method probes the possible svn installation dirs in order to obtain the path of the current svn installation on the local system
								/// </summary>
								String^ GetSubversionInstallationDirectory();

								/// <summary>
								/// Returns true if the given path is a valid subversion binary directory; false otherwise
								/// </summary>
								bool IsSubversionInstallationDirectory(String^ dir);

							public:
							
								/// <summary>
								/// Default destructor of the class
								/// </summary>
								~LibraryLoader();

								/// <summary>
								/// Gets an instance of a specific library
								/// </summary>
								/// <param name="library">The name of the library that has to be loaded</param>
								Library^ GetLibrary(String^ name);

								/// <summary>
								/// Gets an handle of a specific method of a specific library.
								/// </summary>
								/// <param name="library">The name of the library that has to be loaded</param>
								/// <param name="method">The method that has to resolved</param>
								ProcAddress^ GetProcAddress(String^ library, String^ method);

								/// <summary>
								/// Gets an handle of a specific method of a specific library. 
								/// This method evaluates the <see cref="DynamicInvocationAttribute"/>
								/// </summary>
								/// <param name="method">The method that will be analyzed for the <see cref="DynamicInvocationAttribute"/></param>
								/// <exception cref="InvalidOperationException">This Exception will be thrown if the method does not have a valid <see cref="DynamicInvocationAttribute"/></exception>
								ProcAddress^ GetProcAddress(MethodBase^ method);

								/// <summary>
								/// Releases specific libraries and removes the dll instances from the memory
								/// </summary>
								/// <param name="library">The library that shall be released</param>
								void ReleaseLibrary(Library^ library);

								/// <summary>
								/// Releases specific libraries and removes the dll instances from the memory
								/// </summary>
								/// <param name="library">The library that shall be released</param>
								void ReleaseLibrary(String^ name);

								/// <summary>
								/// Releases all libraries. This unloads all currently loaded dll instances from the memory
								/// </summary>
								/// <param name="library">The library that shall be released</param>
								void ReleaseLibraries();

								/// <summary>
								/// Factory method to retrieven an instance of the LibraryLoader
								/// </summary>
								static LibraryLoader^ Instance();
							};
						}
					}
				}
			}
		}
	}
}
