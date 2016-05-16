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
							private ref class SubversionInfoCommand
							{
							private:
								Helpers::SubversionContext^ m_context;
								System::Uri^ m_repoUri;

								System::Guid m_repositoryID;
								System::Uri^ m_repositoryRootURL;

								svn_error_t* SvnInfoReceiverT(void *baton, const char *target, const svn_info_t *info, apr_pool_t *pool);

							public:
								/// <summary>
								/// Creates a new class that can be used to query the repository information
								/// </summary>
								/// <param=context>The context that can be used to access the repository</param>
								/// <param=repository>The full uri for which we want to receive the latest revision number</param>
								SubversionInfoCommand(Helpers::SubversionContext^ context, System::Uri^ repoUri);

								/// <summary>
								/// Executes the command to retrieve the information from subversion
								/// </summary>
								/// <param=context>The real root uri of the subversion repository</param>
								/// <param=repository>The unique ID that can be used to identitfy the repository</param>
								void Execute([Out] System::Uri^% repositoryRoot, [Out] System::Guid% repositoryId);
							};
						}
					}
				}
			}
		}
	}
}