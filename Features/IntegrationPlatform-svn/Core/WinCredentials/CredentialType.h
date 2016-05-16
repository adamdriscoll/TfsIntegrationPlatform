// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

//*****************************************************************************
//  MSDN Reference Link:	http://msdn.microsoft.com/library/aa480470.aspx
//***************************************************************************** 


#pragma once

namespace WinCredentials
{
    public enum class CredentialType
    {
        None = 0,
        Generic = CRED_TYPE_GENERIC,
        DomainPassword = CRED_TYPE_DOMAIN_PASSWORD,
        DomainCertificate = CRED_TYPE_DOMAIN_CERTIFICATE,
        DomainVisiblePassword = CRED_TYPE_DOMAIN_VISIBLE_PASSWORD
    };
}
