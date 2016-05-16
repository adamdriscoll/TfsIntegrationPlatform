#include "Stdafx.h"
#include "LibraryLoader.h"
#include "DI_Svn_Client-1.h"

using namespace System::Reflection;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::LibraryAccess;


Svn_Client^
Svn_Client::Instance()
{
	if(nullptr == m_instance)
	{
		m_instance = gcnew Svn_Client();
	}

	return m_instance;
}


svn_error_t*
Svn_Client::SVN_CLIENT_CREATE_CONTEXT(svn_client_ctx_t **ctx, apr_pool_t *pool)
{
	if(nullptr == m_fpSVN_CLIENT_CREATE_CONTEXT)
	{
		m_fpSVN_CLIENT_CREATE_CONTEXT = LibraryLoader::Instance()->GetProcAddress(MethodInfo::GetCurrentMethod());
	}

	tfpSVN_CLIENT_CREATE_CONTEXT method = (tfpSVN_CLIENT_CREATE_CONTEXT)m_fpSVN_CLIENT_CREATE_CONTEXT->Handle;
	return method(ctx, pool);
}


svn_error_t* 
Svn_Client::SVN_CLIENT_EXPORT4(
	svn_revnum_t*  result_rev,  
	const char*  from,  
	const char*  to, 
	const svn_opt_revision_t *  peg_revision, 
	const svn_opt_revision_t*  revision, 
	svn_boolean_t  overwrite,  
	svn_boolean_t  ignore_externals,  
	svn_depth_t  depth,  
	const char *  native_eol,  
	svn_client_ctx_t *  ctx,  
	apr_pool_t *  pool )						
{
	if(nullptr == m_fpSVN_CLIENT_EXPORT4)
	{
		m_fpSVN_CLIENT_EXPORT4 = LibraryLoader::Instance()->GetProcAddress(MethodInfo::GetCurrentMethod());
	}

	tfpSVN_CLIENT_EXPORT4 method = (tfpSVN_CLIENT_EXPORT4)m_fpSVN_CLIENT_EXPORT4->Handle;
	return method(result_rev, from, to, peg_revision, revision, overwrite, ignore_externals, depth, native_eol, ctx, pool);
}


svn_error_t* 
Svn_Client::SVN_CLIENT_INFO(  
	const char *  path_or_url,  
	const svn_opt_revision_t *  peg_revision,  
	const svn_opt_revision_t *  revision,  
	svn_info_receiver_t  receiver,  
	void *  receiver_baton,  
	svn_boolean_t  recurse,  
	svn_client_ctx_t *  ctx,  
	apr_pool_t *  pool )
{
	if(nullptr == m_fpSVN_CLIENT_INFO)
	{
		m_fpSVN_CLIENT_INFO = LibraryLoader::Instance()->GetProcAddress(MethodInfo::GetCurrentMethod());
	}

	tfpSVN_CLIENT_INFO method = (tfpSVN_CLIENT_INFO)m_fpSVN_CLIENT_INFO->Handle;
	return method(path_or_url, peg_revision, revision, receiver, receiver_baton, recurse, ctx, pool);
}


svn_error_t* 
Svn_Client::SVN_CLIENT_LIST2(
	const char *  path_or_url,  
	const svn_opt_revision_t *  peg_revision,  
	const svn_opt_revision_t *  revision,  
	svn_depth_t  depth,  
	apr_uint32_t  dirent_fields,  
	svn_boolean_t  fetch_locks,  
	svn_client_list_func_t  list_func,  
	void *  baton,  
	svn_client_ctx_t *  ctx,  
	apr_pool_t *  pool)
{
	if(nullptr == m_fpSVN_CLIENT_LIST2)
	{
		m_fpSVN_CLIENT_LIST2 = LibraryLoader::Instance()->GetProcAddress(MethodInfo::GetCurrentMethod());
	}

	tfpSVN_CLIENT_LIST2 method = (tfpSVN_CLIENT_LIST2)m_fpSVN_CLIENT_LIST2->Handle;
	return method(path_or_url, peg_revision, revision, depth, dirent_fields, fetch_locks, list_func, baton, ctx, pool);
}


svn_error_t* 
Svn_Client::SVN_CLIENT_DIFF_SUMMARIZE(
	const char *  path1,  
	const svn_opt_revision_t *  revision1,  
	const char *  path2,  
	const svn_opt_revision_t *  revision2,  
	svn_boolean_t  recurse,  
	svn_boolean_t  ignore_ancestry,  
	svn_client_diff_summarize_func_t  summarize_func,  
	void *  summarize_baton,  
	svn_client_ctx_t *  ctx,  
	apr_pool_t *  pool )
{
	if(nullptr == m_fpSVN_CLIENT_DIFF_SUMMARIZE)
	{
		m_fpSVN_CLIENT_DIFF_SUMMARIZE = LibraryLoader::Instance()->GetProcAddress(MethodInfo::GetCurrentMethod());
	}

	tfpSVN_CLIENT_DIFF_SUMMARIZE method = (tfpSVN_CLIENT_DIFF_SUMMARIZE)m_fpSVN_CLIENT_DIFF_SUMMARIZE->Handle;
	return method(path1, revision1, path2, revision2, recurse, ignore_ancestry, summarize_func, summarize_baton, ctx, pool);
}


svn_error_t* 
Svn_Client::SVN_CLIENT_LS3(  
	apr_hash_t **  dirents,  
	apr_hash_t **  locks,  
	const char *  path_or_url,  
	const svn_opt_revision_t *  peg_revision,  
	const svn_opt_revision_t *  revision,  
	svn_boolean_t  recurse,  
	svn_client_ctx_t *  ctx,  
	apr_pool_t *  pool)
{
	if(nullptr == m_fpSVN_CLIENT_LS3)
	{
		m_fpSVN_CLIENT_LS3 = LibraryLoader::Instance()->GetProcAddress(MethodInfo::GetCurrentMethod());
	}

	tfpSVN_CLIENT_LS3 method = (tfpSVN_CLIENT_LS3)m_fpSVN_CLIENT_LS3->Handle;
	return method(dirents, locks, path_or_url, peg_revision, revision, recurse, ctx, pool);
}


svn_error_t* 
Svn_Client::SVN_CLIENT_LOG4(
	const apr_array_header_t *  targets,    
	const svn_opt_revision_t *  peg_revision,  
	const svn_opt_revision_t *  start,  
	const svn_opt_revision_t *  end,  
	int  limit,  
	svn_boolean_t  discover_changed_paths,  
	svn_boolean_t  strict_node_history,  
	svn_boolean_t  include_merged_revisions,  
	const apr_array_header_t *  revprops,  
	svn_log_entry_receiver_t  receiver,  
	void *  receiver_baton,  
	svn_client_ctx_t *  ctx,  
	apr_pool_t *  pool )
{
	if(nullptr == m_fpSVN_CLIENT_LOG4)
	{
		m_fpSVN_CLIENT_LOG4 = LibraryLoader::Instance()->GetProcAddress(MethodInfo::GetCurrentMethod());
	}

	tfpSVN_CLIENT_LOG4 method = (tfpSVN_CLIENT_LOG4)m_fpSVN_CLIENT_LOG4->Handle;
	return method(targets, peg_revision, start, end, limit, discover_changed_paths, strict_node_history, include_merged_revisions, revprops, receiver, receiver_baton, ctx, pool);
}

svn_error_t* 
Svn_Client::SVN_CLIENT_INFO2(
	const char *path_or_url, 
	const svn_opt_revision_t *peg_revision,
	const svn_opt_revision_t *revision, 
	svn_info_receiver_t receiver,
	void *receiver_baton, 
	svn_depth_t depth, 
	const apr_array_header_t *changelists, 
	svn_client_ctx_t *ctx, 
	apr_pool_t *pool)
{
	if(nullptr == m_fpSVN_CLIENT_INFO2)
	{
		m_fpSVN_CLIENT_INFO2 = LibraryLoader::Instance()->GetProcAddress(MethodInfo::GetCurrentMethod());
	}

	tfpSVN_CLIENT_INFO2 method = (tfpSVN_CLIENT_INFO2)m_fpSVN_CLIENT_INFO2->Handle;
	return method(path_or_url, peg_revision, revision, receiver, receiver_baton, depth, changelists, ctx, pool);
}