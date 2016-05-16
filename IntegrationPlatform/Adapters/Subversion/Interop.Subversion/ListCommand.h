#pragma once

#include <svn_client.h>
#include "Depth.h"

using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;

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
						ref class SubversionClient;

						namespace ObjectModel
						{
							ref class Item;
						};

						namespace Commands
						{
							private ref class ListCommand
							{
							private:
								System::Uri^ m_path;
								long m_revision;
								ObjectModel::Depth m_depth;
								SubversionClient^ m_client;

								List<ObjectModel::Item^>^ m_items;

								svn_error_t* SvnClientListFuncT(void *baton, const char *path, const svn_dirent_t *dirent, const svn_lock_t *lock, const char *abs_path, apr_pool_t *pool);

							public:
								/// <summary>
								/// Creates a helper object that can be used to list the items of a file or folder
								/// </summary>
								/// <param name="client">The context that can be used to access the repository</param>
								/// <param name="path">The path for which we want to retrieve the items</param>
								/// <param name="revision">The revision for which we want to list the items</param>
								/// <param name="path2">The depth of the items that we want to retrieve</param>
								ListCommand(SubversionClient^ client, System::Uri^ path, long revision, ObjectModel::Depth depth);

								/// <summary>
								/// Executes the comparison of the objects
								/// </summary>
								/// <param name="result">The result of the comparison</param>
								void Execute([Out] List<ObjectModel::Item^>^% items);
							};
						}
					}
				}
			}
		}
	}
}