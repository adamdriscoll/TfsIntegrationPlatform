#pragma once

#include <svn_client.h>

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
						namespace ObjectModel
						{
							public enum class Depth
							{
								/// <summary>
								/// Depth undetermined or ignored.
								/// </summary>
								Unknown = svn_depth_unknown,

								/// <summary>
								/// Just the named directory D, no entries.
								/// </summary>
								Empty = svn_depth_empty,
								
								/// <summary>
								/// D + its file children, but not subdirs.
								/// </summary>
								Files = svn_depth_files,

								/// <summary>
								/// D + immediate children (D and its entries).
								/// </summary>
								Immediates = svn_depth_immediates,

								/// <summary>
								/// D + all descendants (full recursion from D). 
								/// </summary>
								Infinity = svn_depth_infinity
							};
						}
					}
				}
			}
		}
	}
}