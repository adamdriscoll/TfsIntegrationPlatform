// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

//*****************************************************************************
//  MSDN Reference Link:	http://msdn.microsoft.com/library/aa480470.aspx
//***************************************************************************** 

#pragma once

namespace WinCredentials
{
    template <typename T>
    class AutoCredentialBuffer
    {
    public:

        AutoCredentialBuffer() :
            m_p(0)
        {
            // Do nothing
        }
        ~AutoCredentialBuffer()
        {
            if (0 != m_p)
            {
                ::CredFree(m_p);
            }
        }

        T m_p;

    private:

        AutoCredentialBuffer(const AutoCredentialBuffer&);
        AutoCredentialBuffer& operator=(const AutoCredentialBuffer&);
    };
}