#include "stdafx.h"
#include "AprPool.h"
#include "LibraryLoader.h"
#include "SvnError.h"
#include "SubversionClient.h"
#include "SubversionContext.h"
#include "Utils.h"

#include "ChangeSet.h"
#include "Item.h"

#include "DiffSummaryCommand.h"
#include "DownloadCommand.h"
#include "ItemInfoCommand.h"
#include "LatestRevisionCommand.h"
#include "ListCommand.h"
#include "LogCommand.h"
#include "SubversionInfoCommand.h"

using namespace System;
using namespace System::Collections::Generic;

using namespace Microsoft::TeamFoundation::Migration::Toolkit;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::Commands;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::Helpers;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::ObjectModel;

SubversionClient::SubversionClient()
{
	Interlocked::Increment(s_references);
}

SubversionClient::~SubversionClient()
{
	int count = Interlocked::Decrement(s_references);
	if(0 == count)
	{
		//This is the last instance. Release all unmanaged ressources that are shared between the instances
		DynamicInvocation::LibraryLoader::Instance()->ReleaseLibraries();
	}

	Disconnect();
}

void 
SubversionClient::Connect(Uri^ repository, NetworkCredential^ credential)
{
	if(nullptr == repository)
	{
		throw gcnew ArgumentNullException("repository");
	}

	try
	{
		m_context = gcnew SubversionContext(credential);
		m_virtualRepositoryRoot = repository;
		
		SubversionInfoCommand^ command = gcnew SubversionInfoCommand(m_context, repository);
		command->Execute(m_repositoryRoot, m_repositoryID);
	}
	catch(Exception^)
	{
		Disconnect();
		throw;
	}
}

void 
SubversionClient::Disconnect()
{
	m_virtualRepositoryRoot = nullptr;
	m_repositoryRoot = nullptr;
	m_repositoryID = Guid::Empty;

	if(nullptr != m_context)
	{
		delete m_context;
		m_context = nullptr;
	}
}

bool
SubversionClient::IsConnected::get()
{
	return nullptr != m_context;
}
						
Guid 
SubversionClient::RepositoryId::get()
{
	if(!IsConnected)
	{
		throw gcnew MigrationException("Subversion Client: There is currently no active connection");
	}

	return m_repositoryID;
}

Uri^ 
SubversionClient::RepositoryRoot::get()
{ 
	if(!IsConnected)
	{
		throw gcnew MigrationException("Subversion Client: There is currently no active connection");
	}

	return m_repositoryRoot;
}

Uri^ 
SubversionClient::VirtualRepositoryRoot::get()
{ 
	if(!IsConnected)
	{
		throw gcnew MigrationException("Subversion Client: There is currently no active connection");
	}

	return m_virtualRepositoryRoot;
}

long 
SubversionClient::GetLatestRevisionNumber(Uri^ path)
{
	if(!IsConnected)
	{
		throw gcnew MigrationException("Subversion Client: There is currently no active connection");
	}

	long result;

	LatestRevisionCommand^ command = gcnew LatestRevisionCommand(m_context, path);
	command->Execute(result);

	return result;
}

SubversionContext^ 
SubversionClient::Context::get()
{ 
	return m_context;
} 

Dictionary<long, ChangeSet^>^
SubversionClient::QueryHistoryRange(Uri^ path, long startRevisionNumber, long endRevisionNumber, bool includeChanges)
{
	if(!IsConnected)
	{
		throw gcnew MigrationException("Subversion Client: There is currently no active connection");
	}

	Dictionary<long, ChangeSet^>^ changesets;

	LogCommand^ command = gcnew LogCommand(this, path, startRevisionNumber, endRevisionNumber, includeChanges);
	command->Execute(changesets);

	return changesets;
}

Dictionary<long, ChangeSet^>^
SubversionClient::QueryHistory(Uri^ path, long startRevisionNumber, int limit, bool includeChanges)
{
	if(!IsConnected)
	{
		throw gcnew MigrationException("Subversion Client: There is currently no active connection");
	}

	Dictionary<long, ChangeSet^>^ changesets;

	LogCommand^ command = gcnew LogCommand(this, path, startRevisionNumber, limit, includeChanges);
	command->Execute(changesets);

	return changesets;
}

Dictionary<long, ChangeSet^>^
SubversionClient::QueryHistory(Uri^ path, int limit, bool includeChanges)
{
	if(!IsConnected)
	{
		throw gcnew MigrationException("Subversion Client: There is currently no active connection");
	}

	Dictionary<long, ChangeSet^>^ changesets;

	LogCommand^ command = gcnew LogCommand(this, path, limit, includeChanges);
	command->Execute(changesets);

	return changesets;
}

List<ItemInfo^>^ 
SubversionClient::QueryItemInfo(Uri^ path, long revision, Depth depth)
{
	if(!IsConnected)
	{
		throw gcnew MigrationException("Subversion Client: There is currently no active connection");
	}

	List<ItemInfo^>^ items;

	ItemInfoCommand^ command = gcnew ItemInfoCommand(this, path, revision, depth);
	command->Execute(items);

	return items;
}

void
SubversionClient::DownloadItem(Uri^ fromPath,  long revision, String^ toPath)
{
	if(!IsConnected)
	{
		throw gcnew MigrationException("Subversion Client: There is currently no active connection");
	}

	DownloadCommand^ command = gcnew DownloadCommand(m_context, fromPath, revision, toPath);
	command->Execute();
}

bool 
SubversionClient::HasContentChange(Uri^ path1, long revision1, System::Uri^ path2, long revision2)
{
	if(!IsConnected)
	{
		throw gcnew MigrationException("Subversion Client: There is currently no active connection");
	}

	bool result;

	DiffSummaryCommand^ command = gcnew DiffSummaryCommand(m_context, path1, revision1, path2, revision2);
	command->AreEqual(result);

	return !result;
}

List<ObjectModel::Item^>^
SubversionClient::GetItems(Uri^ path, long revision, Depth depth)
{
	if(!IsConnected)
	{
		throw gcnew MigrationException("Subversion Client: There is currently no active connection");
	}

	List<Item^>^ items;

	ListCommand^ command = gcnew ListCommand(this, path, revision, depth);
	command->Execute(items);

	return items;
}
