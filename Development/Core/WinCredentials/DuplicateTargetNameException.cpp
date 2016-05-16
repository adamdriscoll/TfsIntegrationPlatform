// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

//*****************************************************************************
//  MSDN Reference Link:	http://msdn.microsoft.com/library/aa480470.aspx
//***************************************************************************** 

#include "stdafx.h"
#include "DuplicateTargetNameException.h"
#include "Strings.h"

using namespace Runtime::Serialization;

WinCredentials::DuplicateTargetNameException::DuplicateTargetNameException(ComponentModel::Win32Exception^ innerException) :
    Exception(Strings::Get("DuplicateTargetNameException.Message"), innerException)
{
    // Do nothing
}

WinCredentials::DuplicateTargetNameException::DuplicateTargetNameException(SerializationInfo^ info,
                                                                 StreamingContext context) :
    Exception(info, context)
{
    // Do nothing
}
