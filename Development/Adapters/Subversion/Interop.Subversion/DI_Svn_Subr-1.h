#pragma once

#include "DynamicInvocationAttribute.h"
#include "Library.h"
#include "svn_cmdline.h"
#include "apr_pools.h"
#include "apr_allocator.h"

using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::DynamicInvocation;

typedef apr_pool_t* (CALLBACK* tfpSVN_POOL_CREATE_EX) (
	apr_pool_t *, 
	apr_allocator_t * );

typedef void (CALLBACK* tfpSVN_POOL_DESTROY) (
	apr_pool_t * );

typedef int (CALLBACK* tfpSVN_CMDLINE_INIT) (
	const char*, FILE * );

typedef svn_error_t* (CALLBACK* tfpSVN_CONFIG_GET_CONFIG) (
	apr_hash_t **, 
	const char *, 
	apr_pool_t * );

typedef svn_error_t* (CALLBACK* tfpSVN_CMDLINE_CREATE_AUTH_BATON) (
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
								);

typedef svn_error_t* (CALLBACK* tfpSVN_UTF_CSTRING_TO_UTF8)(
	const char **dest, 
	const char *src, 
	apr_pool_t *pool);

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
							private ref class Svn_subr
							{
							private:
								ProcAddress^ m_fpSVN_CONFIG_GET_CONFIG;
								ProcAddress^ m_fpSVN_CMDLINE_INIT;	
								ProcAddress^ m_fpSVN_CMDLINE_CREATE_AUTH_BATON;	
								ProcAddress^ m_fpSVN_POOL_CREATE_EX;
								ProcAddress^ m_fpSVN_POOL_DESTROY;
								ProcAddress^ m_fpSVN_UTF_CSTRING_TO_UTF8;
							
								static Svn_subr^ m_instance;

								Svn_subr() { }

							public:
								
								/// <summary>
								/// Gets the actual instance of the library
								/// </summary>
								static Svn_subr^ Instance();

								[DynamicInvocationAttribute("libsvn_subr-1.dll", "svn_cmdline_init")]
								int SVN_CMDLINE_INIT(
									const char* progname, 
									FILE *error_stream);

								[DynamicInvocationAttribute("libsvn_subr-1.dll", "svn_cmdline_create_auth_baton")]
								svn_error_t*  SVN_CMDLINE_CREATE_AUTH_BATON (
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
									) ;

								[DynamicInvocationAttribute("libsvn_subr-1.dll", "svn_pool_create_ex")]
								apr_pool_t* SVN_POOL_CREATE_EX(
									apr_pool_t *parent_pool, 
									apr_allocator_t *allocator);

								//The SVN_POOL_DESTROY method is just a placeholder which will be replaced by the according apr_pool_destory method. We keep it here for convenience reasons
								[DynamicInvocationAttribute("LIBAPR-1.DLL", "_apr_pool_destroy@4")] 
								void SVN_POOL_DESTROY(
									apr_pool_t *pool );

								[DynamicInvocationAttribute("libsvn_subr-1.dll", "svn_config_get_config")]
								svn_error_t* SVN_CONFIG_GET_CONFIG(
									apr_hash_t ** cfg_hash, 
									const char *  config_dir, 
									apr_pool_t *  pool);  

								[DynamicInvocationAttribute("libsvn_subr-1.dll", "svn_utf_string_to_utf8")]
								svn_error_t* SVN_UTF_CSTRING_TO_UTF8(
									const char **dest, 
									const char *src, 
									apr_pool_t *pool);
							};
						}
					}
				}
			}
		}
	}
}