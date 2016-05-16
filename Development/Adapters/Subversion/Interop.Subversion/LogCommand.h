#pragma once

#include <svn_client.h>

using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion;

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
						};

						namespace Commands
						{
							private ref class LogCommand
							{
							private:
								SubversionClient^ m_client;
								System::Uri^ m_path;

								long m_startRevisionNumber;
								long m_endRevisionNumber;
								long m_pegRevisionNumber;

								int m_limit;
								bool m_includeChanges;

								Dictionary<long, ObjectModel::ChangeSet^>^ m_changesets;
								svn_error_t* SvnLogEntryReceiverT(void *baton, svn_log_entry_t *log_entry, apr_pool_t *pool);

							public:
								/// <summary>
								/// Creates a new class that can be used to query the repository information
								/// </summary>
								/// <param name="client">The client object that contains the context to access the repository</param>
								/// <param name="path">The path for which we want to receive the history log</param>
								/// <param name="startRevisionNumber">The start revision number for which we want to query the history log</param>
								/// <param name="endRevisionNumber">The end revision number for which we want to query the history log</param>
								/// <param name="limit">Determines whether we also want to retrieve the changed paths</param>
								LogCommand(SubversionClient^ client, System::Uri^ path, long startRevisionNumber, long endRevisionNumber, bool includeChanges);

								/// <summary>
								/// Creates a new class that can be used to query the repository information
								/// </summary>
								/// <param name="client">The client object that contains the context to access the repository</param>
								/// <param name="path">The path for which we want to receive the history log</param>
								/// <param name="endRevisionNumber">The end revision number for which we want to query the history log</param>
								/// <param name="limit">The maximum number of items which we want to retrieve as result; 0 is infitnity</param>
								/// <param name="includeChanges">Determines whether we also want to retrieve the changed paths</param>
								/// <remark>
								/// Note that the start revision number is the revision number that describes the starting point of the search.
								/// The internal subversion algorithm will then traverse in the past and return these records. Therefore,
								/// if you query having StartRevisionNumber = 5 and Lmit = 0 subversion will return all records between 1 and 5.
								/// It will not return any later changes like 6, 7, 8 ....
								/// </remark>
								LogCommand(SubversionClient^ client, System::Uri^ path, long startRevisionNumber, int limit, bool includeChanges);

								/// <summary>
								/// Creates a new class that can be used to query the repository information
								/// </summary>
								/// <param name="client">The client object that contains the context to access the repository</param>
								/// <param name="path">The path for which we want to receive the history log</param>
								/// <param name="limit">The maximum number of items which we want to retrieve as result; 0 is infitnity</param>
								/// <param name="includeChanges">Determines whether we also want to retrieve the changed paths</param>
								LogCommand(SubversionClient^ client, System::Uri^ path, int limit, bool includeChanges);

								/// <summary>
								/// Executes the command to retrieve the information from subversion
								/// </summary>
								/// <param name="changesets">The changesets that were returned by subversion</param>
								void Execute([Out] Dictionary<long, ObjectModel::ChangeSet^>^% changesets);
							};
						}
					}
				}
			}
		}
	}
}