#include "Stdafx.h"
#include "AprPool.h"
#include "DI_LibApr.h"
#include "DI_Svn_Client-1.h"
#include "LatestRevisionCommand.h"
#include "LibraryLoader.h"
#include "SubversionContext.h"
#include "SvnError.h"

using namespace System::Runtime::InteropServices;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::Commands;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::Helpers;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::LibraryAccess;

[UnmanagedFunctionPointer(CallingConvention::Cdecl)]
delegate svn_error_t* SvnLatestRevNumberDelegate(void *baton, svn_log_entry_t *log_entry, apr_pool_t *pool) ;

LatestRevisionCommand::LatestRevisionCommand(SubversionContext^ context, Uri^ repository)
{
	if(nullptr == context)
		throw gcnew ArgumentNullException("context");

	if(nullptr == repository)
		throw gcnew ArgumentNullException("repository");

	m_context = context;
	m_repository = repository;
}

void 
LatestRevisionCommand::Execute([Out] long% revisionNumber)
{
	svn_opt_revision_t startRevision;
	startRevision.kind = svn_opt_revision_unspecified;
	
	svn_opt_revision_t endRevision;
	endRevision.kind = svn_opt_revision_unspecified;
	
	svn_opt_revision_t pegRevision;
	pegRevision.kind = svn_opt_revision_unspecified;

	SvnLatestRevNumberDelegate^ fp = gcnew SvnLatestRevNumberDelegate(this, &LatestRevisionCommand::SvnLogEntryReceiverT);
	GCHandle gch = GCHandle::Alloc(fp);
	svn_log_entry_receiver_t logEntryReceiverT = static_cast<svn_log_entry_receiver_t>(Marshal::GetFunctionPointerForDelegate(fp).ToPointer());

	AprPool^ pool = gcnew AprPool();
	apr_array_header_t *targets = LibApr::Instance()->AprArrayMake(pool->Handle, 1, sizeof(const char*));
	*(const char**)LibApr::Instance()->AprArrayPush(targets) = pool->CopyString(m_repository->AbsoluteUri);

	m_revision = 0;

	try
	{
		SvnError::Err(Svn_Client::Instance()->SVN_CLIENT_LOG4(targets, &pegRevision, &startRevision, &endRevision, 1, false, false, false, NULL, logEntryReceiverT, NULL, m_context->Handle, pool->Handle));
	}
	finally
	{
		revisionNumber = m_revision;
		gch.Free();
	}
}

svn_error_t* 
LatestRevisionCommand::SvnLogEntryReceiverT(void *baton, svn_log_entry_t *log_entry, apr_pool_t *pool)
{
	m_revision = log_entry->revision;
	return SVN_NO_ERROR;
}
