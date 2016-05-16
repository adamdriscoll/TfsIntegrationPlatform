// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

//*****************************************************************************
//  MSDN Reference Link:	http://msdn.microsoft.com/library/aa480470.aspx
//***************************************************************************** 


#pragma once

namespace WinCredentials
{
    ref class Strings abstract sealed
    {
    public:

        static String^ Get(String^ key);

    private:

        static Strings();
        static initonly Resources::ResourceManager^ m_resources;

    };
}
