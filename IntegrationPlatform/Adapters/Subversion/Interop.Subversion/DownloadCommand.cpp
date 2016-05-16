#include "Stdafx.h"
#include "AprPool.h"
#include "DI_Svn_Client-1.h"
#include "LibraryLoader.h"
#include "SubversionContext.h"
#include "DownloadCommand.h"
#include "SvnError.h"

using namespace System;
using namespace System::Runtime::InteropServices;

using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::Commands;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::LibraryAccess;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::Helpers;
using namespace Microsoft::TeamFoundation::Migration::Toolkit;

DownloadCommand::DownloadCommand(SubversionContext^ context, System::Uri^ fromPath, long revision, String^ toPath)
{
	if(nullptr == context)
	{
		throw gcnew ArgumentNullException("context");
	}

	if(nullptr == fromPath)
	{
		throw gcnew ArgumentNullException("fromPath");
	}

	if(String::IsNullOrEmpty(toPath))
	{
		throw gcnew ArgumentNullException("toPath");
	}

	m_context = context;
	m_fromPath = fromPath;
	m_toPath = toPath;
	m_revision = revision;
}

void 
DownloadCommand::Execute()
{
	svn_revnum_t resultRevision;

	svn_opt_revision_t revision;
	revision.kind = svn_opt_revision_number;
	revision.value.number = (svn_revnum_t)m_revision;

	svn_opt_revision_t pegRevision;
	pegRevision.kind = svn_opt_revision_number;
	pegRevision.value.number = (svn_revnum_t)m_revision;

	AprPool^ pool = gcnew AprPool();
	SvnError::Err(Svn_Client::Instance()->SVN_CLIENT_EXPORT4(&resultRevision, pool->CopyString(m_fromPath->AbsoluteUri), pool->CopyString(m_toPath), &pegRevision, &revision, TRUE, TRUE, svn_depth_empty, NULL, m_context->Handle, pool->Handle));
}