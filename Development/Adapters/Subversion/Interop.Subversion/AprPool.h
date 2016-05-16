#pragma once

#include <svn_pools.h>
#include <svn_client.h>

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
							private ref class AprPool
							{
							private:
								apr_pool_t* m_pool;

							public:
								/// <summary>
								/// Creates a new class that can be used to query the latest revision number of an repository
								/// </summary>
								AprPool();

								/// <summary>
								/// Creates a new class that can be used to query the latest revision number of an repository
								/// </summary>
								/// <param name="parent">The parent memory pool</param>
								AprPool(apr_pool_t* parent);

								/// <summary>
								/// Creates a new class that can be used to query the latest revision number of an repository
								/// </summary>
								/// <param name="parent">The parent memory pool</param>
								AprPool(AprPool^ parent);

								/// <summary>
								/// Default Finalizer
								/// </summary>
								!AprPool();

								/// <summary>
								/// Allocates the required memory in the memory pool. It also copies the bytes of the string into the newly allocated char array
								/// </summary>
								/// <param name="parent">The string for which we have to allocate memory</param>
								char* CopyString(System::String^ s);

								/// <summary>
								/// Default destructor
								/// </summary>
								virtual ~AprPool();
								
								/// <summary>
								/// Gets the handle that can be used to invoce native methods
								/// </summary>
								property apr_pool_t* Handle { apr_pool_t* get(); }
							};
						}
					}
				}
			}
		}
	}
}