#pragma once

#include <svn_client.h>

using namespace System::Net;

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
							ref class AprPool;

							private ref class SubversionContext
							{
							private:
								AprPool^ m_pool;
								NetworkCredential^ m_credential;
								svn_client_ctx_t* m_context;
								
								void Initialize();
								
							public:
								/// <summary>
								/// Creates a new subversion context object
								/// </summary>
								SubversionContext();

								/// <summary>
								/// Creates a new subversion context object
								/// </summary>
								/// <param=credential>The credentials that shall be used to authenticate the user on the repository</param>
								SubversionContext(NetworkCredential^ credential);

								/// <summary>
								/// Default Finalizer
								/// </summary>
								!SubversionContext();

								/// <summary>
								/// Default Destructor
								/// </summary>
								virtual ~SubversionContext();

								/// <summary>
								/// Gets the currently used network credentials
								/// </summary>
								property NetworkCredential^ Credential { NetworkCredential^ get(); }

								/// <summary>
								/// Gets the allocated APR memory pool
								/// </summary>
								property AprPool^ MemoryPool { AprPool^ get(); }

								/// <summary>
								/// The handle of this subversion context that can be used to invoke the native methods
								/// </summary>
								property svn_client_ctx_t* Handle { svn_client_ctx_t* get(); }
							};
						}
					}
				}
			}
		}
	}
}