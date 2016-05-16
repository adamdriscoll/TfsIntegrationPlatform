#pragma once

#include <svn_client.h>

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
						namespace Helpers
						{
							ref class SubversionContext;
						}

						namespace Commands
						{
							private ref class LatestRevisionCommand
							{
							private:
								Helpers::SubversionContext^ m_context;
								System::Uri^ m_repository;
								long m_revision;

								svn_error_t* SvnLogEntryReceiverT(void *baton, svn_log_entry_t *log_entry, apr_pool_t *pool);

							public:
								/// <summary>
								/// Creates a new class that can be used to query the latest revision number of an repository
								/// </summary>
								/// <param=context>The context that can be used to access the repository</param>
								/// <param=repository>The full uri for which we want to receive the latest revision number</param>
								LatestRevisionCommand(Helpers::SubversionContext^ context, System::Uri^ repository);

								/// <summary>
								/// Gets the latest revision number in the subversion repository
								/// </summary>
								/// <param name="revisionNumber">The latest revision number in the repository; 0 if the repository is empty</param>
								/// <returns>The head revision number if the repository contains items; 0 if the repository is empty</returns>
								void Execute([Out] long% revisionNumber);
							};
						}
					}
				}
			}
		}
	}
}