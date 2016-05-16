// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

//*****************************************************************************
//  MSDN Reference Link:	http://msdn.microsoft.com/library/aa480470.aspx
//***************************************************************************** 

#pragma once

namespace WinCredentials
{
    template <typename int Length>
    class AutoPasswordBuffer
    {
    public:

        AutoPasswordBuffer()
        {
            ::ZeroMemory(m_buffer,
                         sizeof (Length));
        }

        ~AutoPasswordBuffer()
        {
            ::SecureZeroMemory(m_buffer,
                               sizeof (Length));
        }

        wchar_t m_buffer[Length];

    };
}
