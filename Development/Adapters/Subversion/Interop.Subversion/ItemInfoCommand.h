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
							ref class ItemInfo;
						};

						namespace Commands
						{
							private ref class ItemInfoCommand
							{
							private:
								SubversionClient^ m_client;
								System::Uri^ m_path;
								long m_pegRevision;
								ObjectModel::Depth m_depth;

								List<ObjectModel::ItemInfo^>^ m_infoItems;
								
								svn_error_t* SvnItemInfoReceiverT(void *baton, const char *path, const svn_info_t *info, apr_pool_t *pool);

							public:
								/// <summary>
								/// Creates a helper class that can be used to query item info objects from subversion
								/// </summary>
								/// <param name="context">The context that can be used to access the repository</param>
								/// <param name="path">The full path of the item that has to be queried</param>
								/// <param name="revision">The revision of the item that has to be queried</param>
								/// <param name="depth">Defines the recursin level</param>
								ItemInfoCommand(SubversionClient^ context, System::Uri^ path, long revision, ObjectModel::Depth depth);

								/// <summary>
								/// Gets the requested item info objects
								/// </summary>
								/// <param name="infoItems">The collection of the info items</param>
								void Execute([Out] List<ObjectModel::ItemInfo^>^% infoItems);
							};
						}
					}
				}
			}
		}
	}
}