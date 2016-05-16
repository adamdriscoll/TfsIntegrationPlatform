#include "Stdafx.h"
#include "LibraryLoader.h"
#include "DI_LibApr.h"

using namespace System::Reflection;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::LibraryAccess;


LibApr^
LibApr::Instance()
{
	if(nullptr == m_instance)
	{
		m_instance = gcnew LibApr();
	}

	return m_instance;
}


apr_status_t
LibApr::Initialize()
{
	if(nullptr == m_fpAprInitialize)
	{
		m_fpAprInitialize = LibraryLoader::Instance()->GetProcAddress(MethodInfo::GetCurrentMethod());
	}

	tfpAprInitialize method = (tfpAprInitialize)m_fpAprInitialize->Handle;
	return method();
}

void
LibApr::Terminate()
{
	if(nullptr == m_fpAprTerminate)
	{
		m_fpAprTerminate = LibraryLoader::Instance()->GetProcAddress(MethodInfo::GetCurrentMethod());
	}

	tfpAprTerminate2 method = (tfpAprTerminate2)m_fpAprTerminate->Handle;
	method();
}


apr_array_header_t* 
LibApr::AprArrayMake(apr_pool_t *p, int nelts, int elt_size)
{
		if(nullptr == m_fpAprArrayMake)
	{
		m_fpAprArrayMake = LibraryLoader::Instance()->GetProcAddress(MethodInfo::GetCurrentMethod());
	}

	tfpAprArrayMake method = (tfpAprArrayMake)m_fpAprArrayMake->Handle;
	return method(p, nelts, elt_size);
}


void *
LibApr::AprArrayPush(apr_array_header_t *arr)
{
	if(nullptr == m_fpAprArrayPush)
	{
		m_fpAprArrayPush = LibraryLoader::Instance()->GetProcAddress(MethodInfo::GetCurrentMethod());
	}

	tfpAprArrayPush method = (tfpAprArrayPush)m_fpAprArrayPush->Handle;
	return method(arr);
}


apr_hash_index_t* 
LibApr::AprHashFirst(apr_pool_t *p, apr_hash_t *ht)
{
	if(nullptr == m_fpAprHashFirst)
	{
		m_fpAprHashFirst = LibraryLoader::Instance()->GetProcAddress(MethodInfo::GetCurrentMethod());
	}

	tfpAprHashFirst method = (tfpAprHashFirst)m_fpAprHashFirst->Handle;
	return method(p, ht);
}


apr_hash_index_t* 
LibApr::AprHashNext(apr_hash_index_t *hi)
{
	if(nullptr == m_fpAprHashNext)
	{
		m_fpAprHashNext = LibraryLoader::Instance()->GetProcAddress(MethodInfo::GetCurrentMethod());
	}

	tfpAprHashNext method = (tfpAprHashNext)m_fpAprHashNext->Handle;
	return method(hi);
}


apr_hash_index_t* 
LibApr::AprHashThis(apr_hash_index_t *hi, const void **key, apr_ssize_t *klen, void **val)
{
	if(nullptr == m_fAprHashThis)
	{
		m_fAprHashThis = LibraryLoader::Instance()->GetProcAddress(MethodInfo::GetCurrentMethod());
	}

	tfpAprHashThis method = (tfpAprHashThis)m_fAprHashThis->Handle;
	return method(hi, key, klen, val);
}

char* 
LibApr::AprPStrDup(apr_pool_t *pool, const char* s)
{
	if(nullptr == m_fAprPStrDup)
	{
		m_fAprPStrDup = LibraryLoader::Instance()->GetProcAddress(MethodInfo::GetCurrentMethod());
	}

	tfpAprPStrDup method = (tfpAprPStrDup)m_fAprPStrDup->Handle;
	return method(pool, s);
}