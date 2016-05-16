#include "stdafx.h"

#include "Change.h"
#include "ChangeSet.h"
#include "DI_LibApr.h"
#include "SubversionClient.h"
#include "Utils.h"

using namespace System;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::Helpers;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::LibraryAccess;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::ObjectModel;
using namespace Microsoft::TeamFoundation::Migration::Toolkit;
using namespace Microsoft::TeamFoundation::Migration::Toolkit::Services;

ChangeSet::ChangeSet(svn_log_entry_t *log_entry, SubversionClient^ client, apr_pool_t* pool)
{	
	if (NULL == log_entry)
	{
		throw gcnew ArgumentNullException("log_entry");
	}

	if(nullptr == client)
	{
		throw gcnew ArgumentNullException("client");
	}

	if(nullptr == pool)
	{
		throw gcnew ArgumentNullException("pool");
	}

	if (NULL == log_entry->revprops)
	{
		throw gcnew ArgumentNullException("log_entry->revprops");
	}

	m_client = client;
	m_revision = log_entry->revision;

	apr_hash_index_t *index;
	void *value;
	const void *key;
	LibApr^ libApr =  LibApr::Instance();
	
	for (index = libApr->AprHashFirst(pool, log_entry->revprops); index; index = libApr->AprHashNext(index))
	{
		libApr->AprHashThis(index, &key, NULL, &value);
		String^ propertyKeyString = Utils::ConvertUTF8ToString((const char*)key);

		if ((String::Equals(propertyKeyString, "svn:log", StringComparison::OrdinalIgnoreCase)))
		{
			m_comment = Utils::ConvertUTF8ToString((const char*)((svn_string_t*)value)->data);
		}
		else if (String::Equals(propertyKeyString, "svn:author", StringComparison::OrdinalIgnoreCase))
		{
			m_author = Utils::ConvertUTF8ToString((const char*)((svn_string_t*)value)->data);
		}
		else if (String::Equals(propertyKeyString, "svn:date", StringComparison::OrdinalIgnoreCase))
		{
			String^ time = Utils::ConvertUTF8ToString((const char*)((svn_string_t*)value)->data);
			if (!DateTime::TryParse(time, m_commitTime))
			{
				TraceManager::TraceError("Fail to parse commitTime '{0}' for revision '{1}'", time, m_revision);
			}
		}
	}
	
	if (NULL != log_entry->changed_paths2)
	{
		m_changes = gcnew List<Change^>();
		for (index = libApr->AprHashFirst(pool, log_entry->changed_paths2); index; index = libApr->AprHashNext(index))
		{
			libApr->AprHashThis(index, &key, NULL, &value);
			svn_log_changed_path2_t* changeDetail = (svn_log_changed_path2_t*)value;
			String^ changePath = gcnew String((const char*)key, 0, strlen((const char*) key), System::Text::Encoding::UTF8);
			m_changes->Add(gcnew Change(this, changePath, changeDetail));
		}
	}
}

long 
ChangeSet::Revision::get() 
{
   return m_revision;
}

DateTime 
ChangeSet::CommitTime::get()
{
	return m_commitTime;
}

String^ 
ChangeSet::Author::get()
{
	return m_author;
}

String^ 
ChangeSet::Comment::get()
{
	return m_comment;
}

List<Change^>^
ChangeSet::Changes::get()
{
	return m_changes;
}

String^
ChangeSet::Repository::get()
{
	return m_client->VirtualRepositoryRoot->ToString();
}

SubversionClient^ 
ChangeSet::Client::get()
{ 
	return m_client;
}

