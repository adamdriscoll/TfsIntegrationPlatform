#include "stdafx.h"
#include <svn_types.h>
#include "Item.h"
#include "Utils.h"

using namespace System;
using namespace msclr::interop;

using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::Helpers;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::ObjectModel;
using namespace Microsoft::TeamFoundation::Migration::Toolkit::Services;

Item::Item(String^ fullpath, const svn_dirent_t* dirent, String^ repositoryRoot)
{
	if(nullptr == fullpath)
	{
		throw gcnew ArgumentNullException("fullpath");
	}
	
	if(NULL == dirent)
	{
		throw gcnew ArgumentNullException("dirent");
	}

	if(nullptr == repositoryRoot)
	{
		throw gcnew ArgumentNullException("repositoryRoot");
	}

	m_fullServerPath = fullpath;
	m_repositoryRoot = repositoryRoot;

	m_size = (long)dirent->size;
	m_createdRev = (long)dirent->created_rev;	
	m_lastAuthor = Utils::ConvertUTF8ToString(dirent->last_author);
	
	switch (dirent->kind)
	{
	case svn_node_kind_t::svn_node_file:
		m_itemType = WellKnownContentType::VersionControlledFile;
		break;
	case svn_node_kind_t::svn_node_dir:
		m_itemType = WellKnownContentType::VersionControlledFolder;
		break;
	default:
		String^ message = String::Format("This case should never happen. Subversion is not able to resolve whether '{0}' is a file or folder.", FullServerPath);
		TraceManager::TraceError(message);
		throw gcnew MigrationException(message);
	}
}

long 
Item::CreatedRev::get()
{ 
	return m_createdRev;
}

ContentType^ 
Item::ItemType::get() 
{
   return m_itemType;
}

long 
Item::Size::get()
{
	return m_size;
}

String^ 
Item::LastAuthor::get()
{
	return m_lastAuthor;
}

String^
Item::Repository::get()
{
	return m_repositoryRoot;
}

String^
Item::FullServerPath::get()
{
	return m_fullServerPath;
}

String^
Item::Path::get()
{
	if(nullptr == m_path)
	{
		m_path = Utils::ExtractPath(Repository, FullServerPath);
	}

	return m_path;
}


