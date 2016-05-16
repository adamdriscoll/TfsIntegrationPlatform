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
						namespace Helpers
						{
							ref class SubversionContext;
						}

						namespace Commands
						{
							private ref class DownloadCommand
							{
							private:
								Helpers::SubversionContext^ m_context;

								Uri^ m_fromPath;
								long m_revision;
								String^ m_toPath;

							public:
								/// <summary>
								/// Creates a new class that can be used to query the repository information
								/// </summary>
								/// <param name="client">The client object that contains the context to access the repository</param>
								/// <param name="fromPath">The full item path in the subversion repository</param>
								/// <param name="revision">The revision of the item that has to be downloaded</param>
								/// <param name="toPath">The full local path where the downloaded item shall be stored</param>
								DownloadCommand(Helpers::SubversionContext^ context, System::Uri^ fromPath, long revision, String^ toPath);

								/// <summary>
								/// Executes the command to retrieve the information from subversion
								/// </summary>
								void Execute();
							};
						}
					}
				}
			}
		}
	}
}