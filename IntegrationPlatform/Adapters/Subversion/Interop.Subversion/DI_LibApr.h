#pragma once

#include "apr.h"
#include "apr_errno.h"
#include "apr_pools.h"
#include "apr_tables.h"
#include "apr_hash.h"
#include "DynamicInvocationAttribute.h"
#include "Library.h"

using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::DynamicInvocation;

typedef apr_status_t (CALLBACK* tfpAprInitialize)();
typedef int (CALLBACK* tfpAprTerminate2)();
typedef apr_array_header_t* (CALLBACK* tfpAprArrayMake) (apr_pool_t *p, int nelts, int elt_size);
typedef void * (CALLBACK* tfpAprArrayPush) (apr_array_header_t *arr);
typedef apr_hash_index_t* (CALLBACK* tfpAprHashFirst) (apr_pool_t *p, apr_hash_t *ht);
typedef apr_hash_index_t* (CALLBACK* tfpAprHashNext) (apr_hash_index_t *hi);
typedef apr_hash_index_t* (CALLBACK* tfpAprHashThis) (apr_hash_index_t *hi, const void **key, apr_ssize_t *klen, void **val);
typedef char* (CALLBACK* tfpAprPStrDup) (apr_pool_t *pool, const char* s);

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
						namespace LibraryAccess
						{
							private ref class LibApr
							{
							private:
								ProcAddress^ m_fpAprTerminate;
								ProcAddress^ m_fpAprInitialize;
								ProcAddress^ m_fpAprArrayMake;
								ProcAddress^ m_fpAprArrayPush;
								ProcAddress^ m_fpAprHashFirst;
								ProcAddress^ m_fpAprHashNext;
								ProcAddress^ m_fAprHashThis;
								ProcAddress^ m_fAprPStrDup;
							
								static LibApr^ m_instance;

								LibApr() { }

							public:
								
								/// <summary>
								/// Gets the actual instance of the library
								/// </summary>
								static LibApr^ Instance();

								[DynamicInvocationAttribute("libapr-1.dll","_apr_initialize@0")]
								apr_status_t Initialize();
							
								[DynamicInvocationAttribute("libapr-1.dll","_apr_terminate2@0")]
								void Terminate();

								[DynamicInvocationAttribute("libapr-1.dll","_apr_array_make@12")]
								apr_array_header_t* AprArrayMake(apr_pool_t *p, int nelts, int elt_size);

								[DynamicInvocationAttribute("libapr-1.dll","_apr_array_push@4")]
								void * AprArrayPush(apr_array_header_t *arr);

								[DynamicInvocationAttribute("libapr-1.dll","_apr_hash_first@8")]
								apr_hash_index_t* AprHashFirst(apr_pool_t *p, apr_hash_t *ht);

								[DynamicInvocationAttribute("libapr-1.dll","_apr_hash_next@4")]
								apr_hash_index_t* AprHashNext(apr_hash_index_t *hi);

								[DynamicInvocationAttribute("libapr-1.dll","_apr_hash_this@16")]
								apr_hash_index_t* AprHashThis(apr_hash_index_t *hi, const void **key, apr_ssize_t *klen, void **val);

								[DynamicInvocationAttribute("libapr-1.dll","_apr_pstrdup@8")]
								char* AprPStrDup(apr_pool_t *pool, const char* s);
							};
						}
					}
				}
			}
		}
	}
}