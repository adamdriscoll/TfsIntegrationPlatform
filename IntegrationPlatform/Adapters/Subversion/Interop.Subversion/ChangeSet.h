#pragma once

#include <svn_client.h>

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
						ref class SubversionClient;

						namespace ObjectModel
						{
							ref class Change;

							public ref class ChangeSet
							{
								private:
									long m_revision;
									String^ m_author;
									String^ m_comment;
									DateTime m_commitTime;
									List<Change^>^ m_changes;
														
									SubversionClient^ m_client;
								
								internal:
									/// <summary>
									/// Gets the subversion client connection that can be used to query more information
									/// </summary>
									property SubversionClient^ Client { SubversionClient^ get(); }

									/// <summary>
									/// Default Constructor
									/// </summary>
									ChangeSet(svn_log_entry_t *log_entry, SubversionClient^ client, apr_pool_t* pool);

								public:
									/// <summary>
									/// Gets the author of the changeset
									/// </summary>
									property String^ Author { String^ get(); }

									/// <summary>
									/// Gets the checkin comment of the changeset
									/// </summary>
									property String^ Comment { String^ get(); }

									/// <summary>
									/// Gets the revision number of this changeset 
									/// </summary>
									property long Revision { long get(); }

									/// <summary>
									/// Gets teh commit time (in UTC) of this changeset
									/// </summary>
									property DateTime CommitTime { DateTime get(); }

									/// <summary>
									/// Gets all the changes if the changed items were queried; Null if the changes were not queried at all
									/// </summary>
									property List<Change^>^ Changes { List<Change^>^ get(); }

									/// <summary>
									/// Gets the URI of the repository
									/// </summary>
									property String^ Repository { String^ get(); }
							};
						}
					}
				}
			}
		}
	}
}