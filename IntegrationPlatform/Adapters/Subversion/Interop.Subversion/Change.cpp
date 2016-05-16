#include "stdafx.h"

#include <msclr/marshal.h>
#include "DI_LibApr.h"
#include "Change.h"
#include "ChangeSet.h"
#include "Depth.h"
#include "ItemInfo.h"
#include "SubversionClient.h"
#include "Utils.h"

using namespace System;
using namespace msclr::interop;

using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::Helpers;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::LibraryAccess;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::ObjectModel;
using namespace Microsoft::TeamFoundation::Migration::Toolkit;
using namespace Microsoft::TeamFoundation::Migration::Toolkit::Services;

Change::Change(ChangeSet^ changeset, String^ changePath, svn_log_changed_path2_t* changeDetail)
{
	if(nullptr == changeset)
	{
		throw gcnew ArgumentNullException("changeSet");
	}

	if(String::IsNullOrEmpty(changePath))
	{
		throw gcnew ArgumentNullException("changePath");
	}

	if(NULL == changeDetail)
	{
		throw gcnew ArgumentNullException("changeDetail");
	}

	m_changeset = changeset;
	m_copyFromRevision = changeDetail->copyfrom_rev;

	m_fullServerPath = Utils::Combine(changeset->Client->RepositoryRoot->ToString(), changePath);
	if (IsCopy)
	{
		String^ path = Utils::ConvertUTF8ToString((const char*)changeDetail->copyfrom_path);
		m_copyFromFullServerPath = Utils::Combine(changeset->Client->RepositoryRoot->ToString(), path);
	}

	m_changeAction = ParseChangeActionChar(changeDetail->action, IsCopy);
	m_nodeKind = changeDetail->node_kind;
}

Change::Change(ChangeSet^ changeset, String^ fullServerPath, String^ copyFromPath, long copyFromRevision, Microsoft::TeamFoundation::Migration::Toolkit::Services::ContentType^ contentType, Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::ObjectModel::ChangeAction changeAction)
{
	m_changeset = changeset;
	m_fullServerPath = fullServerPath;

	m_copyFromPath = copyFromPath;
	m_copyFromRevision = copyFromRevision;
	
	m_itemType = contentType;
	m_changeAction = changeAction;
}

ChangeAction 
Change::ParseChangeActionChar(char actionChar, bool isCopy)
{
	switch (actionChar)
	{
		case 'A':
			return isCopy ? ObjectModel::ChangeAction::Copy : ObjectModel::ChangeAction::Add;
		case 'D':
            return ObjectModel::ChangeAction::Delete;
        case 'M':
            return ObjectModel::ChangeAction::Modify;
        case 'R':
            return ObjectModel::ChangeAction::Replace;
        default:
            TraceManager::TraceError("The change action '{0}' is currently not supported", actionChar);
			throw gcnew NotSupportedException(String::Format("The change action '{0}' is currently not supported", actionChar));
    }
}

ContentType^ 
Change::ParseContentType(svn_node_kind_t nodeKind)
{
	switch (nodeKind)
	{
	case svn_node_kind_t::svn_node_file:
		return WellKnownContentType::VersionControlledFile;
	case svn_node_kind_t::svn_node_dir:
		return WellKnownContentType::VersionControlledFolder;
	case svn_node_kind_t::svn_node_unknown:
		{
			//Sometimes subversion does not report the content type even if it is known. In this case we have to query the content type explicitly 
			//TODO Caching would improve the performance significantly. Currently we are executing a new query for every single item in the repo
			List<ObjectModel::ItemInfo^>^ items =  m_changeset->Client->QueryItemInfo(gcnew Uri(FullServerPath), Changeset->Revision, Depth::Empty);
			if(items->Count > 0)
			{
				return items[0]->ItemType;
			}
			else
			{
				String^ message = String::Format("This case should never happen. Subversion is not able to resolve whether '{0}' is a file or folder.", FullServerPath);
				TraceManager::TraceError(message);
				throw gcnew MigrationException(message);
			}
		}
	default:
		String^ message = String::Format("Subversion reported the svn_node_kind_t value '{0}' which is currently not supported", (int)nodeKind);
		TraceManager::TraceError(message);
		throw gcnew NotSupportedException(message);
	}
}

ContentType^ 
Change::ItemType::get()
{
	if(nullptr == m_itemType)
	{
		m_itemType = ParseContentType(m_nodeKind);
	}

	return m_itemType;
}

ChangeAction 
Change::ChangeAction::get()
{
	return m_changeAction;
}

String^ 
Change::FullServerPath::get()
{
	return m_fullServerPath;
}

String^ 
Change::Path::get()
{
	if(nullptr == m_path)
	{
		m_path = Utils::ExtractPath(m_changeset->Client->VirtualRepositoryRoot->ToString(), FullServerPath);
	}

	return m_path;
}

String^ 
Change::CopyFromFullServerPath::get()
{
	return m_copyFromFullServerPath;
}

String^ 
Change::CopyFromPath::get()
{
	if(nullptr == m_copyFromPath)
	{
		m_copyFromPath = Utils::ExtractPath(m_changeset->Client->VirtualRepositoryRoot->ToString(), CopyFromFullServerPath);
	}

	return m_copyFromPath;
}

bool 
Change::IsCopy::get()
{
	return m_copyFromRevision > 0;
}

long 
Change::CopyFromRevision::get()
{
	return m_copyFromRevision;
}

ChangeSet^ 
Change::Changeset::get()
{
	return m_changeset;
}
