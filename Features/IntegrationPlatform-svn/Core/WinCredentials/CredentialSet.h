// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

//*****************************************************************************
//  MSDN Reference Link:	http://msdn.microsoft.com/library/aa480470.aspx
//***************************************************************************** 

#pragma once

#include "Credential.h"

namespace WinCredentials
{
    public ref class CredentialSet :
        Collections::Generic::IEnumerable<Credential^>
    {
    public:

        [SecurityPermission(SecurityAction::LinkDemand, Flags=SecurityPermissionFlag::UnmanagedCode)]
        CredentialSet();
        
        [SecurityPermission(SecurityAction::LinkDemand, Flags=SecurityPermissionFlag::UnmanagedCode)]
        CredentialSet(String^ filter);

        ~CredentialSet();

#ifdef _DEBUG
        !CredentialSet()
        {
            Debug::Assert(m_disposed,
                          "CredentialSet object was not disposed.");
        }
#endif

        property int Count
        {
            int get();
        }

        property Credential^ default[int]
        {
            Credential^ get(int index);
        }

    private:

        virtual Collections::Generic::IEnumerator<Credential^>^ GetGenericEnumerator() sealed = Collections::Generic::IEnumerable<Credential^>::GetEnumerator;
        virtual Collections::IEnumerator^ GetEnumerator() sealed = Collections::IEnumerable::GetEnumerator;

        void Load(String^ filter);

        void CheckNotDisposed();

        bool m_disposed;

        typedef Collections::Generic::List<Credential^> CredentialList;
        CredentialList^ m_list;

    };
}
