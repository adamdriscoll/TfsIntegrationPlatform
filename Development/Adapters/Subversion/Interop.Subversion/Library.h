#pragma once

#include <Windows.h>
#include <Winbase.h>
#include "ProcAddress.h"

using namespace System;
using namespace System::IO;
using namespace System::Collections::Generic;

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
							private ref class Library
							{
							private:
								//The name of the library
								FileInfo^ m_library;

								//The handle to the library after it has been loaded
								HINSTANCE m_handle;

								//This dictionary caches the loaded symbols of the library
								initonly Dictionary<String^, ProcAddress^>^ m_Addresses;

								/// <summary>
								/// Gets the handle to the library
								/// </summary>
								property HINSTANCE Handle
								{
									HINSTANCE get();
								}

								String^ GetErrorMessage(int error);

							public:

								/// <summary>
								/// Constructor of the LibraryLoader Class
								/// </summary>
								/// <param name="library">The file objects of the library that has to be loaded. The parent directory of the library will be used as search path for the file itslef and all its dependencies</param>
								/// <exception cref="ArgumentNullException">This exception will be thrown if the File object is null</exception>
								/// <exception cref="FileNotFoundException">This exception will be thrown if the file does not exsits</exception>
								Library(FileInfo^ library);

								/// <summary>
								/// Default destructor of the class
								/// </summary>
								~Library();

								/// <summary>
								/// Gets the name of the library. This name also has the extension of the file
								/// </summary>
								property String^ LibraryName
								{
									String^ get();
								}

								/// <summary>
								/// Gets the serach path of the library and all its dependencies
								/// </summary>
								property DirectoryInfo^ LibraryDirectory
								{
									DirectoryInfo^ get();
								}

								/// <summary>
								/// Gets wether the library is alredy laoded or not
								/// </summary>
								property bool IsLoaded
								{
									bool get();
								}

								/// <summary>
								/// Gets the proc address for a specific function
								/// </summary>
								/// <param name="library">The file objects of the library that has to be loaded. The parent directory of the library will be used as search path for the file itslef and all its dependencies</param>
								ProcAddress^ GetProcAddressX(String^ name);

								/// <summary>
								/// Load and initialize the library
								/// </summary
								void Load();

								/// <summary>
								/// Load and initialize the library
								/// </summary>
								void Unload();
							};
						}
					}
				}
			}
		}
	}
}
