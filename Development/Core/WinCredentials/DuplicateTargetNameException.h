// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

//*****************************************************************************
//  MSDN Reference Link:	http://msdn.microsoft.com/library/aa480470.aspx
//***************************************************************************** 

#pragma once

namespace WinCredentials
{
    [Serializable]
    public ref class DuplicateTargetNameException : Exception
    {
    internal:

        DuplicateTargetNameException(ComponentModel::Win32Exception^ innerException);

    protected:

        DuplicateTargetNameException(Runtime::Serialization::SerializationInfo^ info,
                                     Runtime::Serialization::StreamingContext context);
    };
}
