#include "Stdafx.h"
#include "AprPool.h"
#include "LibraryLoader.h"
#include "DI_Svn_Subr-1.h"
#include "DI_LibApr.h"

using namespace System;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::Helpers;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::LibraryAccess;
using namespace Microsoft::TeamFoundation::Migration::Toolkit;

AprPool::AprPool()
{
	m_pool = Svn_subr::Instance()->SVN_POOL_CREATE_EX(NULL, NULL);
	if(NULL == m_pool)
	{
		//This case should never happen because apr installs an abort handle which terminates the application in case of Out Of Memory
		TraceManager::TraceError("Subversion Interop: An error occurd while creating a new memory pool");
		throw gcnew MigrationException("Subversion Interop: An error occurd while creating a new memory pool");
	}
}

AprPool::AprPool(apr_pool_t* parent)
{
	m_pool = Svn_subr::Instance()->SVN_POOL_CREATE_EX(parent, NULL);	
	if(NULL == m_pool)
	{
		//This case should never happen because apr installs an abort handle which terminates the application in case of Out Of Memory
		TraceManager::TraceError("Subversion Interop: An error occurd while creating a new memory pool");
		throw gcnew MigrationException("Subversion Interop: An error occurd while creating a new memory pool");
	}
}

AprPool::AprPool(AprPool^ parent)
{
	if(nullptr == parent)
	{
		m_pool = Svn_subr::Instance()->SVN_POOL_CREATE_EX(NULL, NULL);
	}
	else
	{
		pin_ptr<apr_pool_t> pp = parent->Handle;
		m_pool = Svn_subr::Instance()->SVN_POOL_CREATE_EX(pp, NULL);
	}

	if(NULL == m_pool)
	{
		//This case should never happen because apr installs an abort handle which terminates the application in case of Out Of Memory
		TraceManager::TraceError("Subversion Interop: An error occurd while creating a new memory pool");
		throw gcnew MigrationException("Subversion Interop: An error occurd while creating a new memory pool");
	}
}

AprPool::~AprPool()
{
	if(NULL != m_pool)
	{
		Svn_subr::Instance()->SVN_POOL_DESTROY(m_pool);
		m_pool = NULL;
	}
}

AprPool::!AprPool()
{
	if(NULL != m_pool)
	{
		Svn_subr::Instance()->SVN_POOL_DESTROY(m_pool);
		m_pool = NULL;
	}
}

char* 
AprPool::CopyString(String^ s)
{
	if(nullptr == s)
		return NULL;

	array<Byte>^ barray = System::Text::Encoding::UTF8->GetBytes(s);
	pin_ptr<Byte> pinbarray =  &barray[0];
	const char* pczArray = reinterpret_cast<const char*>(pinbarray);
	
	return LibApr::Instance()->AprPStrDup(Handle, pczArray); 
}

apr_pool_t*
AprPool::Handle::get()
{
	return m_pool;
}
