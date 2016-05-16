// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

//*****************************************************************************
//  MSDN Reference Link:	http://msdn.microsoft.com/library/aa480470.aspx
//***************************************************************************** 


#pragma once

namespace WinCredentials
{
    class AutoPasswordBstr
    {
    public:

        AutoPasswordBstr() :
            m_bstr(0)
        {
            // Do nothing
        }

        ~AutoPasswordBstr()
        {
            if (0 != m_bstr)
            {
                ::SecureZeroMemory(m_bstr,
                                   ::SysStringByteLen(m_bstr));

                ::SysFreeString(m_bstr);
            }
        }

        BSTR m_bstr;

    private:

        AutoPasswordBstr(const AutoPasswordBstr&);
        AutoPasswordBstr& operator=(const AutoPasswordBstr&);

    };
}
