#include "Stdafx.h"
#include "ProcAddress.h"

using namespace System::Threading;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::DynamicInvocation;

ProcAddress::ProcAddress(FARPROC procAddress, String^ procAddressName)
{
	if(nullptr == procAddress)
	{
		throw gcnew ArgumentNullException("procAddress");
	}

	if(String::IsNullOrEmpty(procAddressName))
	{
		throw gcnew ArgumentNullException("procAddressName");
	}

	m_procAddress = procAddress;
	m_procAddressName = procAddressName;

	m_referenceCount = gcnew int(1);
}

ProcAddress::ProcAddress(ProcAddress^ address)
{
	if(nullptr == address)
	{
		throw gcnew ArgumentNullException("address");
	}

	Interlocked::Increment(*(address->m_referenceCount));

	m_referenceCount = address->m_referenceCount;
	m_procAddress = address->m_procAddress;
	m_procAddressName = address->m_procAddressName;
}

ProcAddress::~ProcAddress()
{
	Interlocked::Decrement(*m_referenceCount);
}

FARPROC 
ProcAddress::Handle::get()
{
	return m_procAddress;
}

String^
ProcAddress::ProcAddressName::get()
{
	return m_procAddressName;
}

int
ProcAddress::References::get()
{
	return *m_referenceCount;
}