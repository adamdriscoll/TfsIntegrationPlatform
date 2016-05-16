#pragma once

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
							public enum class ChangeAction
							{
								/// <summary>
								/// The item has been added to the repository
								/// </summary>
								Add,

								/// <summary>
								/// The item has been added to the repository by copying an already existing file
								/// </summary>
								Copy,
								
								/// <summary>
								/// The item has been deleted from the repository
								/// </summary>
								Delete,

								/// <summary>
								/// The content of the item has been changed
								/// </summary>
								Modify,

								/// <summary>
								/// The item has been replaced by an other item
								/// </summary>
								Replace
							};
						}
					}
				}
			}
		}
	}
}