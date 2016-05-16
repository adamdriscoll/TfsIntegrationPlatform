#pragma once

#include <svn_client.h>
#include "ChangeAction.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;
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
							ref class ChangeSet;

							public ref class Change
							{
								private:
									ChangeSet^ m_changeset;
									ChangeAction m_changeAction;
									
									ContentType^ m_itemType;
									svn_node_kind_t m_nodeKind;
									
									String^ m_fullServerPath;
									String^ m_path;
									
									long m_copyFromRevision;
									String^ m_copyFromFullServerPath;
									String^ m_copyFromPath;

									ChangeAction ParseChangeActionChar(char actionChar, bool isCopy);
									ContentType^ ParseContentType(svn_node_kind_t nodeKind);
									
								internal:

									/// <summary>
									/// Create a new Change Object
									/// </summary>
									/// <param name="changeSet">The changeset to which this change belongs to</param>
									/// <param name="changePath">The relative item path within the repository</param>
									/// <param name="changeDetail">Additional attributes of this change like the change action</param>
									Change(ChangeSet^ changeset, String^ changePath, svn_log_changed_path2_t* changeDetail);

								public:
									/// <summary>
									/// Create a new Change Object
									/// </summary>
									/// <param name="changeSet">The changeset to which this change belongs to</param>
									/// <param name="fullServerPath">The full path of the item in the subversion repository</param>
									/// <param name="copyFromPath">The full path of the item from which this item was copied from</param>
									/// <param name="copyFromRevision">The revision from which this item has been copied from</param>
									/// <param name="contentType">The content type of this item</param>
									/// <param name="changeAction">The actual change of this item</param>
									Change(ChangeSet^ changeset, String^ fullServerPath, String^ copyFromPath, long copyFromRevision, ContentType^ itemType, ChangeAction changeAction);

									/// <summary>
									/// Determines whether the changed item is a file or a folder
									/// </summary>
									property ChangeSet^ Changeset
									{
										ChangeSet^ get();
									}

									/// <summary>
									/// Gets the content type of this item. An item can be either a file or a folder
									/// </summary>
									property ContentType^ ItemType
									{
										ContentType^ get();
									}

									/// <summary>
									/// Gets the change operation of this item
									/// </summary>
									/// <exception cref="NotSupportedException">Will be thrown if subversion reported an unsupported change action</exception>
									property ChangeAction ChangeAction
									{
										Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::ObjectModel::ChangeAction get();
									}

									/// <summary>
									/// Gets the absolute <see cref="Uri"/> of the item in the svn repository.
									/// The <see cref="Uri"/> object also contains the url to the svn repository
									/// <para/>
									/// Example Value: svn://localhost/repos/Svn2Tfs/Folder/File.txt
									/// </summary>
									property String^ FullServerPath
									{
										String^ get();
									}

									/// <summary>
									/// Gets the relative <see cref="Uri"/> of the item within the repository.
									/// The <see cref="Uri"/> object does not contain the repository url. 
									/// It just contains the path part of the item in the repository.
									/// The Path always starts with an slash
									/// <para/>
									/// Example Value: /Folder/File.txt
									/// </summary>
									property String^ Path
									{
										String^ get();
									}

									/// <summary>
									/// Determines whether this file has a copy relation. A copied file has a branch relation to the file origin
									/// </summary>
									property bool IsCopy
									{
										bool get();
									}

									/// <summary>
									/// Gets the absolute <see cref="Uri"/> of the branch source in the svn repository.
									/// The <see cref="Uri"/> object also contains the url to the svn repository
									/// <para/>
									/// Example Value: svn://localhost/repos/Svn2Tfs/Folder/File.txt
									/// </summary>
									property String^ CopyFromFullServerPath
									{
										String^ get();
									}

									/// <summary>
									/// Gets the relative <see cref="Uri"/> of the branch source item within the repository.
									/// The <see cref="Uri"/> object does not contain the repository url. 
									/// It just contains the path part of the item in the repository.
									/// The Path always starts with an slash
									/// <para/>
									/// Example Value: /Folder/File.txt
									/// </summary>
									property String^ CopyFromPath
									{
										String^ get();
									}

									/// <summary>
									/// Gets the revision of the source file or folder witch is the source of this copy
									/// </summary>
									property long CopyFromRevision
									{
										long get();
									}
							};
						}
					}
				}
			}
		}
	}
}