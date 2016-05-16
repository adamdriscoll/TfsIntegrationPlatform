#include "Stdafx.h"
#include "AprPool.h"
#include "DI_LibApr.h"
#include "DI_Svn_Client-1.h"
#include "DI_Svn_Subr-1.h"
#include "LibraryLoader.h"
#include "SubversionContext.h"
#include "SvnError.h"

using namespace System::Net;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::Helpers;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::LibraryAccess;

SubversionContext::SubversionContext()
{
	m_pool = nullptr;
	m_credential = nullptr;
	
	Initialize();
}

SubversionContext::SubversionContext(NetworkCredential^ credential)
{
	m_pool = nullptr;
	m_credential = credential;
	
	Initialize();
}

SubversionContext::!SubversionContext()
{
	if(nullptr != m_pool)
	{
		delete m_pool;
		m_pool = nullptr;
	}
}

SubversionContext::~SubversionContext()
{
	if(nullptr != m_pool)
	{
		delete m_pool;
		m_pool = nullptr;
	}
}

NetworkCredential^ 
SubversionContext::Credential::get()
{ 
	return m_credential;
}

svn_client_ctx_t* 
SubversionContext::Handle::get()
{ 
	return m_context;
}

AprPool^ 
SubversionContext::MemoryPool::get()
{ 
	return m_pool;
}

void
SubversionContext::Initialize()
{
	Svn_subr::Instance()->SVN_CMDLINE_INIT("Integration Platform Subversion Adapter", nullptr);
	
	m_pool = gcnew AprPool();
	pin_ptr<svn_client_ctx_t*> context = &m_context;
	pin_ptr<apr_pool_t> pool = m_pool->Handle;

	SvnError::Err(Svn_Client::Instance()->SVN_CLIENT_CREATE_CONTEXT(context, pool));
	SvnError::Err(Svn_subr::Instance()->SVN_CONFIG_GET_CONFIG(&((*context)->config), NULL, pool));
	
	m_context->log_msg_func = NULL;
	m_context->log_msg_baton = NULL;

	if(nullptr != m_credential)
	{
		SvnError::Err(Svn_subr::Instance()->SVN_CMDLINE_CREATE_AUTH_BATON(&(m_context->auth_baton), true, m_pool->CopyString(m_credential->UserName),  m_pool->CopyString(m_credential->Password), NULL, false, false, NULL,  NULL, NULL, pool));
	}
	else
	{
		SvnError::Err(Svn_subr::Instance()->SVN_CMDLINE_CREATE_AUTH_BATON(&(m_context->auth_baton), true, NULL,  NULL, NULL, false, false, NULL,  NULL, NULL, pool));
	}
}