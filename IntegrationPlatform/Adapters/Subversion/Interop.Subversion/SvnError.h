#pragma once

#include "svn_types.h"

using namespace System;

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
						public ref class SvnError
						{
						private:
							/// <summary>
							/// The constructor is private to ensure that the class cannot be instantiiated
							/// </summary>
							SvnError() { }

							/// <summary>
							/// This method is used to log all svn related errors
							/// </summary>
							static void LogSvnError(svn_error_t* svnError);
							
						public:							
							/// <summary>
							/// Analyzes the svnError object
							/// </summary>
							static void Err(svn_error_t* svnError);
						};
					}
				}
			}
		}
	}
}
