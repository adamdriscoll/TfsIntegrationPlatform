#include "Stdafx.h"
#include <msclr/marshal.h>
#include "ItemInfo.h"
#include "Utils.h"

using namespace System;
using namespace msclr::interop;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::Helpers;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::ObjectModel;
using namespace Microsoft::TeamFoundation::Migration::Toolkit;
using namespace Microsoft::TeamFoundation::Migration::Toolkit::Services;

ItemInfo::ItemInfo(const svn_info_t* info)
{
	if(NULL == info)
	{
		throw gcnew ArgumentNullException("info");
	}

	m_uri = gcnew System::Uri(Utils::ConvertUTF8ToString((const char*)info->URL));
	m_repositoryUri = gcnew System::Uri(Utils::ConvertUTF8ToString((const char*)info->repos_root_URL));

	m_revision = (long)info->rev;

	switch(info->kind)
	{
		case svn_node_kind_t::svn_node_file:
			m_itemType = WellKnownContentType::VersionControlledFile;
			break;
		case svn_node_kind_t::svn_node_dir:
			m_itemType = WellKnownContentType::VersionControlledFolder;
			break;
		default:
			String^ message = String::Format("Subversion reported the svn_node_kind_t value '{0}' which is currently not supported", (int)info->kind);
			TraceManager::TraceError(message);
			throw gcnew NotSupportedException(message);
	}
}

System::Uri^ 
ItemInfo::Uri::get() 
{ 
	return m_uri;
}

long 
ItemInfo::Revision::get() 
{
	return m_revision;
}

ContentType^
ItemInfo::ItemType::get() 
{
	return m_itemType;
}

System::Uri^ 
ItemInfo::RepositoryRootUrl::get() 
{
	return m_repositoryUri;
}