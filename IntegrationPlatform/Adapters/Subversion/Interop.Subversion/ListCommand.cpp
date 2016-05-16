#include "Stdafx.h"
#include "AprPool.h"
#include "DI_LibApr.h"
#include "DI_Svn_Client-1.h"
#include "Item.h"
#include "LibraryLoader.h"
#include "ListCommand.h"
#include "SubversionClient.h"
#include "SubversionContext.h"
#include "SvnError.h"
#include "Utils.h"

using namespace System::Runtime::InteropServices;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::Commands;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::Helpers;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::LibraryAccess;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::ObjectModel;

[UnmanagedFunctionPointer(CallingConvention::Cdecl)]
delegate svn_error_t* SvnClientListFuncTDelegate(void *baton, const char *path, const svn_dirent_t *dirent, const svn_lock_t *lock, const char *abs_path, apr_pool_t *pool);

ListCommand::ListCommand(SubversionClient^ client, System::Uri^ path, long revision, ObjectModel::Depth depth)
{
	if(nullptr == client)
		throw gcnew ArgumentNullException("client");

	if(nullptr == path)
		throw gcnew ArgumentNullException("path");

	m_client = client;
	m_path = path;
	m_revision = revision;
	m_depth = depth;
}

void 
ListCommand::Execute([Out] List<Item^>^% items)
{
	svn_opt_revision_t revision;
	revision.kind = svn_opt_revision_number;
	revision.value.number = (svn_revnum_t)m_revision;

	svn_opt_revision_t pegRevision;
	pegRevision.kind = svn_opt_revision_number;
	pegRevision.value.number = (svn_revnum_t)m_revision;

	AprPool^ pool = gcnew AprPool();
	SvnClientListFuncTDelegate^ fp = gcnew SvnClientListFuncTDelegate(this, &ListCommand::SvnClientListFuncT);
	GCHandle gch = GCHandle::Alloc(fp);
	svn_client_list_func_t  receiver = static_cast<svn_client_list_func_t >(Marshal::GetFunctionPointerForDelegate(fp).ToPointer());

	m_items = gcnew List<Item^>();

	try
	{
		SvnError::Err(Svn_Client::Instance()->SVN_CLIENT_LIST2(pool->CopyString(m_path->AbsoluteUri), &pegRevision, &revision, (svn_depth_t)m_depth, SVN_DIRENT_ALL, false, receiver, NULL, m_client->Context->Handle, pool->Handle));
	}
	finally
	{
		items = m_items;
		gch.Free();
	}
}

svn_error_t* 
ListCommand::SvnClientListFuncT(void *baton, const char *path, const svn_dirent_t *dirent, const svn_lock_t *lock, const char *abs_path, apr_pool_t *pool)
{
	//Construct the full path
	String^ fullpath = Utils::Combine(m_client->RepositoryRoot->ToString(), Utils::ConvertUTF8ToString(abs_path));
	if(NULL != path)
	{
		fullpath = Utils::Combine(fullpath, Utils::ConvertUTF8ToString(path));
	}

	Item^ item = gcnew Item(fullpath, dirent, m_client->VirtualRepositoryRoot->ToString());
	m_items->Add(item);

	return SVN_NO_ERROR;
}
