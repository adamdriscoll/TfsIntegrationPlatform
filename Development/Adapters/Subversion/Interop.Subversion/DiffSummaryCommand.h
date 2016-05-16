#pragma once

#include <svn_client.h>

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
						namespace Helpers
						{
							ref class SubversionContext;
						}

						namespace Commands
						{
							private ref class DiffSummaryCommand
							{
							private:
								System::Uri^ m_path1;
								long m_revision1;

								System::Uri^ m_path2;
								long m_revision2;
								
								Helpers::SubversionContext^ m_context;

								bool m_result;
								svn_error_t* SvnClientDiffSummarizeFuncT(const svn_client_diff_summarize_t *diff, void *baton, apr_pool_t *pool);

							public:
								/// <summary>
								/// Creates a helper object that can be used to diff two subversion items
								/// </summary>
								/// <param name="context">The context that can be used to access the repository</param>
								/// <param name="path1">The first item used for the comparison</param>
								/// <param name="revision1">The revision of the first item</param>
								/// <param name="path2">The second item for the comparison</param>
								/// <param name="revision2">The revision of the second item</param>
								DiffSummaryCommand(Helpers::SubversionContext^ context, System::Uri^ path1, long revision1, System::Uri^ path2, long revision2);

								/// <summary>
								/// Executes the comparison of the objects
								/// </summary>
								/// <param name="result">The result of the comparison</param>
								void AreEqual([Out] bool% result);
							};
						}
					}
				}
			}
		}
	}
}