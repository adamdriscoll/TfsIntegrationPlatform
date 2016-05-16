#pragma once

#include "Depth.h"

using namespace System;
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
						namespace Helpers
						{
							ref class SubversionContext;
						};

						namespace ObjectModel
						{
							ref class Item;
							ref class ItemInfo;
							ref class ChangeSet;
						}

						public ref class SubversionClient
						{
						private:
							static int s_references = 0;
							
							Helpers::SubversionContext^ m_context;
							
							Uri^ m_virtualRepositoryRoot;
							Uri^ m_repositoryRoot;
							Guid m_repositoryID;
							
						internal:

							/// <summary>
							/// Gets the context associated with this client session object
							/// </summary>
							property Helpers::SubversionContext^ Context { Helpers::SubversionContext^ get(); } 

						public:
							/// <summary>
							/// Default Constructor
							/// </summary>
							SubversionClient();
							
							/// <summary>
							/// Default destructor
							/// </summary>
							~SubversionClient();

							/// <summary>
							/// Establishes a connection to the repository
							/// </summary>
							/// <param name="repository">The uri of the repository</param>
							/// <param name="credential">The credentials that will be used to connect to the repository</param>
							void Connect(System::Uri^ repository, System::Net::NetworkCredential^ credential);

							/// <summary>
							/// Closes the connection and release all ressources
							/// </summary>
							void Disconnect();

							/// <summary>
							/// Gets wether the client is currently connected to the repository
							/// </summary>
							property bool IsConnected { bool get(); }

							/// <summary>
							/// Gets the latest revision number in the subversion repository
							/// </summary>
							/// <returns>The head revision number if the repository contains items; 0 if the repository is empty</returns>
							long GetLatestRevisionNumber(Uri^ path);

							/// <summary>
							/// Gets the unique identifier of the repository
							/// </summary>
							property Guid RepositoryId { Guid get(); }

							/// <summary>
							/// Gets the root uri of the repository
							/// </summary>
							property Uri^ RepositoryRoot { Uri^ get(); }

							/// <summary>
							/// The virtual root directory is the uri that has been used to connect to subversion. This is not necessarily the real root of the repository. 
							/// However, the migration is using this directory as actual root directory
							/// </summary>
							property Uri^ VirtualRepositoryRoot{ Uri^ get(); }

							/// <summary>
							/// Queries the history log for a specific item in the subversion repository
							/// </summary>
							/// <param name="path">The path for which we want to receive the history log</param>
							/// <param name="startRevisionNumber">The start revision number for which we want to query the history log</param>
							/// <param name="endRevisionNumber">The end revision number for which we want to query the history log</param>
							/// <param name="limit">Determines whether we also want to retrieve the changed paths</param>
							Dictionary<long, ObjectModel::ChangeSet^>^ QueryHistoryRange(Uri^ path, long startRevisionNumber, long endRevisionNumber, bool includeChanges);

							/// <summary>
							/// Queries the history log for a specific item in the subversion repository
							/// </summary>
							/// <param name="path">The path for which we want to receive the history log</param>
							/// <param name="startRevisionNumber">The start revision number for which we want to query the history log</param>
							/// <param name="limit">The maximum number of items which we want to retrieve as result; 0 is infitnity</param>
							/// <param name="includeChanges">Determines whether we also want to retrieve the changed paths</param>
							/// <remark>
							/// Note that the start revision number is the revision number that describes the starting point of the search.
							/// The internal subversion algorithm will then traverse in the past and return these records. Therefore,
							/// if you query having StartRevisionNumber = 5 and Lmit = 0 subversion will return all records between 1 and 5.
							/// It will not return any later changes like 6, 7, 8 ....
							/// </remark>
							Dictionary<long, ObjectModel::ChangeSet^>^ QueryHistory(Uri^ path, long startRevisionNumber, int limit, bool includeChanges);

							/// <summary>
							/// Queries the history log for a specific item in the subversion repository
							/// </summary>
							/// <param name="path">The path for which we want to receive the history log</param>
							/// <param name="limit">The maximum number of items which we want to retrieve as result; 0 is infitnity</param>
							/// <param name="includeChanges">Determines whether we also want to retrieve the changed paths</param>
							Dictionary<long, ObjectModel::ChangeSet^>^ QueryHistory(Uri^ path, int limit, bool includeChanges);

							/// <summary>
							/// Queries the item info for a specific item at a specific revision
							/// </summary>
							/// <param name="path">The full path of the item that has to be queried</param>
							/// <param name="revision">The revision of the item that has to be queried</param>
							/// <param name="depth">Defines the recursin level</param>
							List<ObjectModel::ItemInfo^>^ QueryItemInfo(Uri^ path, long revision, ObjectModel::Depth depth);
							
							/// <summary>
							/// Download an item at a specific revision to a local destination path. 
							/// </summary>
							/// <param name="fromPath">The full item path in the subversion repository</param>
							/// <param name="revision">The revision of the item that has to be downloaded</param>
							/// <param name="toPath">The full local path where the downloaded item shall be stored</param>
							void DownloadItem(Uri^ fromPath, long revision, String^ toPath);

							/// <summary>
							/// Compares two subversion item at specific revisions for content change
							/// </summary>
							/// <param name="path1">The first item used for the comparison</param>
							/// <param name="revision1">The revision of the first item</param>
							/// <param name="path2">The second item for the comparison</param>
							/// <param name="revision2">The revision of the second item</param>
							/// <returns>True if the items do not have any content change; false otherwise</returns>
							bool HasContentChange(System::Uri^ path1, long revision1, System::Uri^ path2, long revision2);

							/// <summary>
							/// Lists the items below of specific path
							/// </summary>
							/// <param name="path">The path for which the items shall be listed</param>
							/// <param name="revision">The revision for which the items shall be listed</param>
							/// <param name="depth">The recursion type used to retrieve the items</param>
							/// <returns>A list with all retrieved items</returns>
							List<ObjectModel::Item^>^ SubversionClient::GetItems(System::Uri^ path, long revision, ObjectModel::Depth depth);
						};
					}
				}
			}
		}
	}
}
