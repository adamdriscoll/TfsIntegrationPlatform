// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

//*****************************************************************************
//  MSDN Reference Link:	http://msdn.microsoft.com/library/aa480470.aspx
//***************************************************************************** 

#include "stdafx.h"
#include "Strings.h"

String^ WinCredentials::Strings::Get(String^ key)
{
    Debug::Assert(nullptr != key);
    Debug::Assert(0 < key->Length);

    String^ value = m_resources->GetString(key);

    Debug::Assert(nullptr != value);
    return value;
}

static WinCredentials::Strings::Strings()
{
    Reflection::Assembly^ assembly = Reflection::Assembly::GetExecutingAssembly();

    m_resources = gcnew Resources::ResourceManager("WinCredentials.Strings",
                                                   assembly);
}
