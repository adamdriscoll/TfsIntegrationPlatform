#include "Stdafx.h"
#include "AprPool.h"
#include "ChangeSet.h"
#include "DI_LibApr.h"
#include "DI_Svn_Client-1.h"
#include "ItemInfo.h"
#include "LibraryLoader.h"
#include "ItemInfoCommand.h"
#include "SubversionClient.h"
#include "SubversionContext.h"
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
delegate svn_error_t* SvnItemInfoReceiverTDelegate(void *baton, const char *path, const svn_info_t *info, apr_pool_t *pool);

ItemInfoCommand::ItemInfoCommand(SubversionClient^ client, System::Uri^ path, long revision, Depth depth)
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
	m_pegRevision = revision;
	m_depth = depth;
}
								
void 
ItemInfoCommand::Execute([Out] List<ItemInfo^>^% infoItems)
{
	m_infoItems = gcnew List<ItemInfo^>();

	svn_opt_revision_t revision;
	revision.kind = svn_opt_revision_unspecified;

	svn_opt_revision_t pegRevision;
	if(m_pegRevision >= 0)
	{
		pegRevision.kind = svn_opt_revision_number;
		pegRevision.value.number = m_pegRevision;
	}
	else
	{
		pegRevision.kind = svn_opt_revision_unspecified;
	}

	AprPool^ pool = gcnew AprPool();
	SvnItemInfoReceiverTDelegate^ fp = gcnew SvnItemInfoReceiverTDelegate(this, &ItemInfoCommand::SvnItemInfoReceiverT);
	GCHandle gch = GCHandle::Alloc(fp);
	svn_info_receiver_t  receiver = static_cast<svn_info_receiver_t>(Marshal::GetFunctionPointerForDelegate(fp).ToPointer());

	try
	{
		SvnError::Err(Svn_Client::Instance()->SVN_CLIENT_INFO2(pool->CopyString(m_path->AbsoluteUri) , &pegRevision, &revision, receiver, NULL, (svn_depth_t)m_depth, NULL,  m_client->Context->Handle, pool->Handle));
	}
	finally
	{
		infoItems = m_infoItems;
		gch.Free();
	}
}

svn_error_t* 
ItemInfoCommand::SvnItemInfoReceiverT(void *baton, const char *path, const svn_info_t *info, apr_pool_t *pool)
{
	//TODO we shouldnt throw any exception within a manged function point callback. Create a proper svnerr object and return the exception and rethrow it there 
	//rather than unwinding all the unmangaded stack which  might not be aware about the exception thrown. Additionally we should also catch all exceptions here
	//and marshall those back

	ItemInfo^ infoObj = gcnew ItemInfo(info);
	m_infoItems->Add(infoObj);
	
	return SVN_NO_ERROR;
}