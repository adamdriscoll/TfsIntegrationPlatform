#pragma once

#include "DynamicInvocationAttribute.h"
#include "Library.h"
#include "apr_pools.h"
#include "apr_allocator.h"
#include "svn_client.h"

using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::DynamicInvocation;

typedef svn_error_t* (CALLBACK* tfpSVN_CLIENT_CREATE_CONTEXT) (
	svn_client_ctx_t **ctx, 
	apr_pool_t *pool );

typedef svn_error_t* (CALLBACK* tfpSVN_CLIENT_EXPORT4) (
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
	apr_pool_t *  pool );  

typedef svn_error_t* (CALLBACK* tfpSVN_CLIENT_INFO) (  
	const char *  path_or_url,  
	const svn_opt_revision_t *  peg_revision,  
	const svn_opt_revision_t *  revision,  
	svn_info_receiver_t  receiver,  
	void *  receiver_baton,  
	svn_boolean_t  recurse,  
	svn_client_ctx_t *  ctx,  
	apr_pool_t *  pool );

typedef svn_error_t* (CALLBACK* tfpSVN_CLIENT_LIST2) (  
	const char *  path_or_url,  
	const svn_opt_revision_t *  peg_revision,  
	const svn_opt_revision_t *  revision,  
	svn_depth_t  depth,  
	apr_uint32_t  dirent_fields,  
	svn_boolean_t  fetch_locks,  
	svn_client_list_func_t  list_func,  
	void *  baton,  
	svn_client_ctx_t *  ctx,  
	apr_pool_t *  pool);

typedef svn_error_t* (CALLBACK* tfpSVN_CLIENT_LS3) (  
	apr_hash_t **  dirents,  
	apr_hash_t **  locks,  
	const char *  path_or_url,  
	const svn_opt_revision_t *  peg_revision,  
	const svn_opt_revision_t *  revision,  
	svn_boolean_t  recurse,  
	svn_client_ctx_t *  ctx,  
	apr_pool_t *  pool);

typedef svn_error_t* (CALLBACK* tfpSVN_CLIENT_LOG4) (  
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
	apr_pool_t *  pool );

typedef svn_error_t* (CALLBACK* tfpSVN_CLIENT_DIFF_SUMMARIZE) (
	const char *path1,  
	const svn_opt_revision_t *revision1,  
	const char *path2,  
	const svn_opt_revision_t *revision2,  
	svn_boolean_t recurse,  
	svn_boolean_t ignore_ancestry,  
	svn_client_diff_summarize_func_t summarize_func,  
	void *summarize_baton,  
	svn_client_ctx_t *ctx,  
	apr_pool_t *pool ); 

typedef svn_error_t* (CALLBACK* tfpSVN_CLIENT_INFO2) (
	const char *path_or_url, 
	const svn_opt_revision_t *peg_revision, 
	const svn_opt_revision_t *revision, 
	svn_info_receiver_t receiver, 
	void *receiver_baton, 
	svn_depth_t depth, 
	const apr_array_header_t *changelists, 
	svn_client_ctx_t *ctx, 
	apr_pool_t *pool );

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
							private ref class Svn_Client
							{
							private:
								ProcAddress^ m_fpSVN_CLIENT_CREATE_CONTEXT;
								ProcAddress^ m_fpSVN_CLIENT_EXPORT4;
								ProcAddress^ m_fpSVN_CLIENT_INFO;
								ProcAddress^ m_fpSVN_CLIENT_LIST2;
								ProcAddress^ m_fpSVN_CLIENT_LS3;
								ProcAddress^ m_fpSVN_CLIENT_LOG4;
								ProcAddress^ m_fpSVN_CLIENT_DIFF_SUMMARIZE;
								ProcAddress^ m_fpSVN_CLIENT_INFO2;
							
								static Svn_Client^ m_instance;
								Svn_Client() { }

							public:
								
								/// <summary>
								/// Gets the actual instance of the library
								/// </summary>
								static Svn_Client^ Instance();

								[DynamicInvocationAttribute("libsvn_client-1.dll", "svn_client_create_context")]
								svn_error_t* SVN_CLIENT_CREATE_CONTEXT(
									svn_client_ctx_t **ctx, 
									apr_pool_t *pool );

								[DynamicInvocationAttribute("libsvn_client-1.dll", "svn_client_export4")]
								svn_error_t* SVN_CLIENT_EXPORT4(
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
									apr_pool_t *  pool ); 

								[DynamicInvocationAttribute("libsvn_client-1.dll", "svn_client_info")]
								svn_error_t* SVN_CLIENT_INFO(  
									const char *  path_or_url,  
									const svn_opt_revision_t *  peg_revision,  
									const svn_opt_revision_t *  revision,  
									svn_info_receiver_t  receiver,  
									void *  receiver_baton,  
									svn_boolean_t  recurse,  
									svn_client_ctx_t *  ctx,  
									apr_pool_t *  pool ); 

								[DynamicInvocationAttribute("libsvn_client-1.dll", "svn_client_list2")]
								svn_error_t* SVN_CLIENT_LIST2(  
									const char *  path_or_url,  
									const svn_opt_revision_t *  peg_revision,  
									const svn_opt_revision_t *  revision,  
									svn_depth_t  depth,  
									apr_uint32_t  dirent_fields,  
									svn_boolean_t  fetch_locks,  
									svn_client_list_func_t  list_func,  
									void *  baton,  
									svn_client_ctx_t *  ctx,  
									apr_pool_t *  pool);

								[DynamicInvocationAttribute("libsvn_client-1.dll", "svn_client_ls3")]
								svn_error_t* SVN_CLIENT_LS3(  
									apr_hash_t **  dirents,  
									apr_hash_t **  locks,  
									const char *  path_or_url,  
									const svn_opt_revision_t *  peg_revision,  
									const svn_opt_revision_t *  revision,  
									svn_boolean_t  recurse,  
									svn_client_ctx_t *  ctx,  
									apr_pool_t *  pool);

								[DynamicInvocationAttribute("libsvn_client-1.dll", "svn_client_log4")]
								svn_error_t* SVN_CLIENT_LOG4(  
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
									apr_pool_t *  pool );

								[DynamicInvocationAttribute("libsvn_client-1.dll", "svn_client_diff_summarize")]
								svn_error_t* SVN_CLIENT_DIFF_SUMMARIZE(
									const char *path1,  
									const svn_opt_revision_t *revision1,  
									const char *path2,  
									const svn_opt_revision_t *revision2,  
									svn_boolean_t  recurse,  
									svn_boolean_t  ignore_ancestry,  
									svn_client_diff_summarize_func_t summarize_func,  
									void *summarize_baton,  
									svn_client_ctx_t *ctx,  
									apr_pool_t *pool );

								[DynamicInvocationAttribute("libsvn_client-1.dll", "svn_client_info2")]
								svn_error_t* SVN_CLIENT_INFO2(
									const char *path_or_url, 
									const svn_opt_revision_t *peg_revision, 
									const svn_opt_revision_t *revision, 
									svn_info_receiver_t receiver, 
									void *receiver_baton, svn_depth_t depth, 
									const apr_array_header_t *changelists, 
									svn_client_ctx_t *ctx, 
									apr_pool_t *pool);
							};
						}
					}
				}
			}
		}
	}
}