// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

//*****************************************************************************
//  MSDN Reference Link:	http://msdn.microsoft.com/library/aa480470.aspx
//***************************************************************************** 

#pragma once

namespace WinCredentials
{
    public enum class CredentialPersistence
    {
        None = 0,
        Session = CRED_PERSIST_SESSION,
        LocalComputer = CRED_PERSIST_LOCAL_MACHINE,
        Enterprise = CRED_PERSIST_ENTERPRISE
    };
}
