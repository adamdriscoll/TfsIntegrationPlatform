#include "Stdafx.h"
#include "AprPool.h"
#include "DI_Svn_Client-1.h"
#include "LibraryLoader.h"
#include "SubversionContext.h"
#include "SubversionInfoCommand.h"
#include "SvnError.h"
#include "Utils.h"

using namespace System;
using namespace System::Runtime::InteropServices;

using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::Commands;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::LibraryAccess;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::Helpers;
using namespace Microsoft::TeamFoundation::Migration::Toolkit;

[UnmanagedFunctionPointer(CallingConvention::Cdecl)]
delegate svn_error_t* SvnInfoReceiverTDelegate(void *baton, const char *target, const svn_info_t *info, apr_pool_t *pool);

SubversionInfoCommand::SubversionInfoCommand(SubversionContext^ context, System::Uri^ repoUri)
{
	if(nullptr == context)
	{
		throw gcnew ArgumentNullException("context");
	}

	if(nullptr == repoUri)
	{
		throw gcnew ArgumentNullException("repoUri");
	}

	m_context = context;
	m_repoUri = repoUri;
}

void 
SubversionInfoCommand::Execute([Out] System::Uri^% repositoryRoot, [Out] System::Guid% repositoryId)
{
	AprPool^ pool = gcnew AprPool();

	svn_opt_revision_t pegRevision;
	pegRevision.kind = svn_opt_revision_head;

	svn_opt_revision_t revision;
	revision.kind = svn_opt_revision_head;

	SvnInfoReceiverTDelegate^ fp = gcnew SvnInfoReceiverTDelegate(this, &SubversionInfoCommand::SvnInfoReceiverT);
	GCHandle gch = GCHandle::Alloc(fp);
	svn_info_receiver_t receiver = static_cast<svn_info_receiver_t>(Marshal::GetFunctionPointerForDelegate(fp).ToPointer());

	m_repositoryRootURL = nullptr;
	m_repositoryID = Guid::Empty;

	try
	{	
		SvnError::Err(Svn_Client::Instance()->SVN_CLIENT_INFO(pool->CopyString(m_repoUri->AbsoluteUri), &pegRevision, &revision, receiver, NULL, FALSE, m_context->Handle, m_context->MemoryPool->Handle));
	}
	finally
	{
		repositoryRoot = m_repositoryRootURL;
		repositoryId = m_repositoryID;
		
		gch.Free();
	}
}

svn_error_t *
SubversionInfoCommand::SvnInfoReceiverT(void *baton, const char *target, const svn_info_t *info, apr_pool_t *pool)
{
	//TODO we shouldnt throw any exception within a manged function point callback. Create a proper svnerr object and return the exception and rethrow it there 
	//rather than unwinding all the unmangaded stack which  might not be aware about the exception thrown. Additionally we should also catch all exceptions here
	//and marshall those back

	if (NULL == info)
	{
		TraceManager::TraceError("Svn dynamic invocation error: SvnInfoReceiverT returns Null svn_info_t object");
	}
	else
	{
		if (info->repos_root_URL)
		{
			m_repositoryRootURL = gcnew Uri(Utils::ConvertUTF8ToString(info->repos_root_URL));
		}
		else
		{	
			TraceManager::TraceError("Svn dynamic invocation error: SvnInfoReceiverT returns Null repos_root_URL object");
		}

		if (info->repos_UUID)
		{
			m_repositoryID = Guid(Utils::ConvertUTF8ToString(info->repos_UUID));
		}
		else
		{		
			TraceManager::TraceError("Svn dynamic invocation error: SvnInfoReceiverT returns Null repos_UUID object");
		}
	}

	return SVN_NO_ERROR;
}

