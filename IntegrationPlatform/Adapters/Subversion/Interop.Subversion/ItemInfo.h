#pragma once

#include <svn_client.h>

using namespace System;
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
						namespace ObjectModel
						{
							public ref class ItemInfo
							{
								private:
									System::Uri^ m_uri;
									System::Uri^ m_repositoryUri;

									long m_revision;
									ContentType^ m_itemType;

									//ContentType^ ParseContentType(svn_node_kind_t nodeKind);
									
								internal:

									/// <summary>
									/// Create a new SVN Info Object
									/// </summary>
									/// <param name="changeSet">The changeset to which this change belongs to</param>
									/// <param name="changePath">The relative item path within the repository</param>
									/// <param name="changeDetail">Additional attributes of this change like the change action</param>
									ItemInfo(const svn_info_t* info);

								public:
									
									/// <summary>
									/// Gets the URI of the item
									/// </summary>
									property System::Uri^ Uri { System::Uri^ get(); }

									/// <summary>
									/// Gets the revision of the object. 
									/// </summary>
									property long Revision { long get(); }

									/// <summary>
									/// Gets the content type of this item. An item can be either a file or a folder
									/// </summary>
									property ContentType^ ItemType { ContentType^ get(); }

									/// <summary>
									/// Gets the root url of the repository
									/// </summary>
									property System::Uri^ RepositoryRootUrl { System::Uri^ get(); }

									//Note: This struct offers a couple more properties. We do not need these right now. Therefore we do not export them
							};
						}
					}
				}
			}
		}
	}
}