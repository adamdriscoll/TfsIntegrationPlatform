#include "Stdafx.h"
#include "AprPool.h"
#include "ChangeSet.h"
#include "DI_LibApr.h"
#include "DI_Svn_Client-1.h"
#include "LibraryLoader.h"
#include "SubversionClient.h"
#include "SubversionContext.h"
#include "LogCommand.h"
#include "SvnError.h"

using namespace System;
using namespace System::Runtime::InteropServices;

using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::Commands;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::LibraryAccess;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::ObjectModel;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::Helpers;
using namespace Microsoft::TeamFoundation::Migration::Toolkit;

[UnmanagedFunctionPointer(CallingConvention::Cdecl)]
delegate svn_error_t* SvnLogEntryReceiverTDelegate(void *baton, svn_log_entry_t *log_entry, apr_pool_t *pool);

LogCommand::LogCommand(SubversionClient^ client, System::Uri^ path, long startRevisionNumber, long endRevisionNumber, bool includeChanges)
{
	if(nullptr == client)
	{
		throw gcnew ArgumentNullException("client");
	}

	if(nullptr == path)
	{
		throw gcnew ArgumentNullException("path");
	}

	m_client = client;
	m_path = path;

	m_startRevisionNumber = startRevisionNumber;
	m_endRevisionNumber = endRevisionNumber;
	m_pegRevisionNumber = -1;

	m_limit = 0;
	m_includeChanges = includeChanges;
}

LogCommand::LogCommand(SubversionClient^ client, System::Uri^ path, long startRevisionNumber, int limit, bool includeChanges)
{
	if(nullptr == client)
	{
		throw gcnew ArgumentNullException("client");
	}

	if(nullptr == path)
	{
		throw gcnew ArgumentNullException("path");
	}

	m_client = client;
	m_path = path;

	m_startRevisionNumber = -1;
	m_endRevisionNumber = -1;
	m_pegRevisionNumber = startRevisionNumber;

	m_limit = limit;
	m_includeChanges = includeChanges;
}

LogCommand::LogCommand(SubversionClient^ client, System::Uri^ path, int limit, bool includeChanges)
{
	if(nullptr == client)
	{
		throw gcnew ArgumentNullException("client");
	}

	if(nullptr == path)
	{
		throw gcnew ArgumentNullException("path");
	}

	m_client = client;
	m_path = path;

	m_startRevisionNumber = -1;
	m_endRevisionNumber = -1;
	m_pegRevisionNumber = -1;

	m_limit = limit;
	m_includeChanges = includeChanges;
}

void 
LogCommand::Execute([Out] Dictionary<long, ChangeSet^>^% changesets)
{
	m_changesets = gcnew Dictionary<long, ChangeSet^>();

	svn_opt_revision_t startRevision;
	if(m_startRevisionNumber >= 0)
	{
		startRevision.kind = svn_opt_revision_number;
		startRevision.value.number = m_startRevisionNumber;
	}
	else
	{
		startRevision.kind = svn_opt_revision_unspecified;
	}

	svn_opt_revision_t endRevision;
	if(m_endRevisionNumber >= 0)
	{
		endRevision.kind = svn_opt_revision_number;
		endRevision.value.number = m_endRevisionNumber;
	}
	else
	{
		endRevision.kind = svn_opt_revision_unspecified;
	}
	
	svn_opt_revision_t pegRevision;
	if(m_pegRevisionNumber >= 0)
	{
		pegRevision.kind = svn_opt_revision_number;
		pegRevision.value.number = m_pegRevisionNumber;
	}
	else
	{
		pegRevision.kind = svn_opt_revision_unspecified;
	}

	AprPool^ pool = gcnew AprPool();
	apr_array_header_t *targets;
	targets = LibApr::Instance()->AprArrayMake(pool->Handle, 1, sizeof(const char*));
	*(const char**)LibApr::Instance()->AprArrayPush(targets) = pool->CopyString(m_path->AbsoluteUri);

	SvnLogEntryReceiverTDelegate^ fp = gcnew SvnLogEntryReceiverTDelegate(this, &LogCommand::SvnLogEntryReceiverT);
	GCHandle gch = GCHandle::Alloc(fp);
	svn_log_entry_receiver_t receiver = static_cast<svn_log_entry_receiver_t>(Marshal::GetFunctionPointerForDelegate(fp).ToPointer());

	try
	{
		SvnError::Err(Svn_Client::Instance()->SVN_CLIENT_LOG4(targets, &pegRevision, &startRevision, &endRevision, m_limit, m_includeChanges, false, false, NULL, receiver, NULL, m_client->Context->Handle, pool->Handle));
	}
	finally
	{
		changesets = m_changesets;
		gch.Free();
	}
}

svn_error_t*
LogCommand::SvnLogEntryReceiverT(void *baton, svn_log_entry_t *log_entry, apr_pool_t *pool) 
{
	//TODO we shouldnt throw any exception within a manged function point callback. Create a proper svnerr object and return the exception and rethrow it there 
	//rather than unwinding all the unmangaded stack which  might not be aware about the exception thrown. Additionally we should also catch all exceptions here
	//and marshall those back

	ChangeSet^ changeSet = gcnew ChangeSet(log_entry, m_client, pool);
	m_changesets->Add(changeSet->Revision, changeSet);

	return SVN_NO_ERROR;
}