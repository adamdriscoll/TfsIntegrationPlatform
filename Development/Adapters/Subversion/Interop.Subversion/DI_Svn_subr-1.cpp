#include "Stdafx.h"
#include "LibraryLoader.h"
#include "DI_Svn_Subr-1.h"

using namespace System::Reflection;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::LibraryAccess;


Svn_subr^
Svn_subr::Instance()
{
	if(nullptr == m_instance)
	{
		m_instance = gcnew Svn_subr();
	}

	return m_instance;
}


int
Svn_subr::SVN_CMDLINE_INIT(const char* progname, FILE *error_stream)
{
	if(nullptr == m_fpSVN_CMDLINE_INIT)
	{
		m_fpSVN_CMDLINE_INIT = LibraryLoader::Instance()->GetProcAddress(MethodInfo::GetCurrentMethod());
	}

	tfpSVN_CMDLINE_INIT method = (tfpSVN_CMDLINE_INIT)m_fpSVN_CMDLINE_INIT->Handle;
	return method(progname, error_stream);
}


apr_pool_t* 
Svn_subr::SVN_POOL_CREATE_EX(apr_pool_t *parent_pool, apr_allocator_t *allocator)	
{
	if(nullptr == m_fpSVN_POOL_CREATE_EX)
	{
		m_fpSVN_POOL_CREATE_EX = LibraryLoader::Instance()->GetProcAddress(MethodInfo::GetCurrentMethod());
	}

	tfpSVN_POOL_CREATE_EX method = (tfpSVN_POOL_CREATE_EX)m_fpSVN_POOL_CREATE_EX->Handle;
	return method(parent_pool, allocator);
}


void
Svn_subr::SVN_POOL_DESTROY( apr_pool_t *pool )
{
	if(nullptr == m_fpSVN_POOL_DESTROY)
	{
		m_fpSVN_POOL_DESTROY = LibraryLoader::Instance()->GetProcAddress(MethodInfo::GetCurrentMethod());
	}

	tfpSVN_POOL_DESTROY method = (tfpSVN_POOL_DESTROY)m_fpSVN_POOL_DESTROY->Handle;
	method(pool);
}


svn_error_t* 
Svn_subr::SVN_CONFIG_GET_CONFIG(apr_hash_t **  cfg_hash, const char *  config_dir, apr_pool_t *  pool)
{
	if(nullptr == m_fpSVN_CONFIG_GET_CONFIG)
	{
		m_fpSVN_CONFIG_GET_CONFIG = LibraryLoader::Instance()->GetProcAddress(MethodInfo::GetCurrentMethod());
	}

	tfpSVN_CONFIG_GET_CONFIG method = (tfpSVN_CONFIG_GET_CONFIG)m_fpSVN_CONFIG_GET_CONFIG->Handle;
	return method(cfg_hash, config_dir, pool);
}


svn_error_t*  
Svn_subr::SVN_CMDLINE_CREATE_AUTH_BATON (
	svn_auth_baton_t **  ab,  
	svn_boolean_t  non_interactive,  
	const char *  username,  
	const char *  password,  
	const char *  config_dir,  
	svn_boolean_t  no_auth_cache,  
	svn_boolean_t  trust_server_cert,  
	svn_config_t *  cfg,  
	svn_cancel_func_t  cancel_func,  
	void *  cancel_baton,  
	apr_pool_t *  pool 
	)
{
	if(nullptr == m_fpSVN_CMDLINE_CREATE_AUTH_BATON)
	{
		m_fpSVN_CMDLINE_CREATE_AUTH_BATON = LibraryLoader::Instance()->GetProcAddress(MethodInfo::GetCurrentMethod());
	}

	tfpSVN_CMDLINE_CREATE_AUTH_BATON method = (tfpSVN_CMDLINE_CREATE_AUTH_BATON)m_fpSVN_CMDLINE_CREATE_AUTH_BATON->Handle;
	return method(ab, non_interactive, username, password, config_dir, no_auth_cache, trust_server_cert, cfg, cancel_func, cancel_baton, pool);
}

svn_error_t* 
Svn_subr::SVN_UTF_CSTRING_TO_UTF8(const char **dest, const char *src, apr_pool_t *pool) 
{
	if(nullptr == m_fpSVN_UTF_CSTRING_TO_UTF8)
	{
		m_fpSVN_UTF_CSTRING_TO_UTF8 = LibraryLoader::Instance()->GetProcAddress(MethodInfo::GetCurrentMethod());
	}

	tfpSVN_UTF_CSTRING_TO_UTF8 method = (tfpSVN_UTF_CSTRING_TO_UTF8)m_fpSVN_UTF_CSTRING_TO_UTF8->Handle;
	return method(dest, src, pool);
}

							

