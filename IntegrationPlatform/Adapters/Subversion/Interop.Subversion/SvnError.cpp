#include "stdafx.h"
#include <msclr\marshal.h>
#include <svn_error_codes.h>
#include "SvnError.h"
#include "Utils.h"

using namespace System;
using namespace msclr::interop;
using namespace Microsoft::TeamFoundation::Migration::Toolkit;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::Helpers;


void 
SvnError::LogSvnError(svn_error_t* svnError)
{
	try
	{
		String^ message = NULL != svnError->message ? Utils::ConvertUTF8ToString(svnError->message) : String.Empty;
		String^ file = NULL != svnError->file ? Utils::ConvertUTF8ToString(svnError->file) : String.Empty;
		TraceManager::TraceError("An error occured while accessing svn: '{0}' - Ln: {1}: {2}", message, svnError->line, file);

		if (NULL != svnError->child)
		{
			LogSvnError(svnError->child);
		}
		// Todo also output apr_status_t and apr_pool_t
	}
	catch (Exception ^) { /* Don't throw when trying to writing to log file. */ }
}


void
SvnError::Err(svn_error_t* svnError)
{
	if (NULL != svnError)
	{
		LogSvnError(svnError);

		//Decode the supported error types
		switch(svnError->apr_err)
		{
		case SVN_ERR_RA_NOT_AUTHORIZED:
			throw gcnew UnauthorizedAccessException();
			break;
		default:
			throw gcnew MigrationException(NULL != svnError->message ? Utils::ConvertUTF8ToString(svnError->message) : String.Empty);
			break;
		}
	}
}