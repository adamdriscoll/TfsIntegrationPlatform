#include "Stdafx.h"
#include "AprPool.h"
#include "DI_LibApr.h"
#include "DI_Svn_Client-1.h"
#include "DiffSummaryCommand.h"
#include "LibraryLoader.h"
#include "SubversionContext.h"
#include "SvnError.h"

using namespace System::Runtime::InteropServices;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::Commands;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::Helpers;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::LibraryAccess;

[UnmanagedFunctionPointer(CallingConvention::Cdecl)]
delegate svn_error_t* SvnClientDiffSummarizeFuncTDelegate(const svn_client_diff_summarize_t *diff, void *baton, apr_pool_t *pool);

DiffSummaryCommand::DiffSummaryCommand(SubversionContext^ context, Uri^ path1, long revision1, Uri^ path2, long revision2)
{
	if(nullptr == context)
		throw gcnew ArgumentNullException("context");

	if(nullptr == path1)
		throw gcnew ArgumentNullException("path1");

	if(nullptr == path2)
		throw gcnew ArgumentNullException("path2");

	m_context = context;

	m_path1 = path1;
	m_path2 = path2;

	m_revision1 = revision1;
	m_revision2 = revision2;
}

void 
DiffSummaryCommand::AreEqual([Out] bool% result)
{
	svn_opt_revision_t revisionT1;
	revisionT1.kind = svn_opt_revision_number;
	revisionT1.value.number = (svn_revnum_t)m_revision1;

	svn_opt_revision_t revisionT2;
	revisionT2.kind = svn_opt_revision_number;
	revisionT2.value.number = (svn_revnum_t)m_revision2;

	AprPool^ pool = gcnew AprPool();
	SvnClientDiffSummarizeFuncTDelegate^ fp = gcnew SvnClientDiffSummarizeFuncTDelegate(this, &DiffSummaryCommand::SvnClientDiffSummarizeFuncT);
	GCHandle gch = GCHandle::Alloc(fp);
	svn_client_diff_summarize_func_t receiver = static_cast<svn_client_diff_summarize_func_t>(Marshal::GetFunctionPointerForDelegate(fp).ToPointer());

	try
	{
		m_result = true; //Initialize as equal because the diff function will not be called on equality
		SvnError::Err(Svn_Client::Instance()->SVN_CLIENT_DIFF_SUMMARIZE(pool->CopyString(m_path1->AbsoluteUri), &revisionT1, pool->CopyString(m_path2->AbsoluteUri), &revisionT2, false, false, receiver, NULL, m_context->Handle, pool->Handle));
	}
	finally
	{
		result = m_result;
		gch.Free();
	}
}

svn_error_t*
DiffSummaryCommand::SvnClientDiffSummarizeFuncT(const svn_client_diff_summarize_t *diff, void *baton, apr_pool_t *pool)
{
	m_result = svn_client_diff_summarize_kind_normal == diff->summarize_kind;
	return SVN_NO_ERROR;
}