#pragma once

#include "apr_time.h"
#include "svn_types.h"

using namespace System;

using namespace Microsoft::TeamFoundation::Migration::Toolkit;
using namespace Microsoft::TeamFoundation::Migration::Toolkit::Services;

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
						namespace Helpers
						{
							private ref class Utils abstract sealed
							{
								public:
									static String^ Seperator = "/";
									static array<Char>^ SeperatorCharArray = {'/'};
								
									/// <summary>
									/// This mehtod combines the root path and the relative path to a new URI object
									/// <para/>
									/// Example:
									/// <code>
									/// root: svn://localhost/repos/svn2tofs
									/// relative: /Folder/File.txt
									/// result: svn://localhost/repos/svn2tofs/Folder/File.txt
									/// </code>
									/// </summary>
									/// <param name="root">The root uri that is used for the concat operation</param>
									/// <param name="relative">The relative path that has to be added</param>
									/// <returns>Returns an combined <see cref="Uri"/> object</returns>
									static String^ Combine(String^ root, String^ relative);
								
									/// <summary>
									/// This method will convert an fully qualified uri of a file to a folder to an relative uri that points
									/// to the same file folder relative based on the defined baseuri
									/// <para/>
									/// Example:
									/// <code>
									/// fullUri: svn://localhost/repos/svn2tofs/Folder/File.txt
									/// baseUri: svn://localhost/repos/svn2tfs/
									/// result: /Folder/File.txt
									/// </code>
									/// </summary>
									/// <param name="baseUri">The root uri that is used to substitute the path from the full URI</param>
									/// <param name="fullUri">The uri that points to a file or folder below the uri</param>
									/// <returns>Returns an <see cref="Uri"/> that contains only the path fragment </returns>
									static String^ ExtractPath(String^ baseUri, String^ fullUri);

									/// <summary>
									/// Converts an UTF8 encoded standard c string (char*) to System::String
									/// </summary>
									/// <param name="value">The value that shall be converted</param>
									static String^ ConvertUTF8ToString(const char* value);
							};
						}
					}
				}
			}
		}
	}
}