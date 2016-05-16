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
    class AutoGdiObject
    {
    public:

        AutoGdiObject() :
            m_handle(0)
        {
            // Do nothing
        }

        ~AutoGdiObject()
        {
            if (0 != m_handle)
            {
                BOOL result = ::DeleteObject(m_handle);
                Debug::Assert(0 != result);
            }
        }

        T m_handle;

    private:

        AutoGdiObject(const AutoGdiObject&);
        AutoGdiObject& operator=(const AutoGdiObject&);

    };
}
