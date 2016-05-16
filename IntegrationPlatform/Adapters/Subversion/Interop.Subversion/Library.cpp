#include "stdafx.h"
#include <tchar.h>
#include <msclr\marshal.h>
#include "Library.h"
#include "LibraryUnloadException.h"

using namespace msclr::interop;
using namespace System::Reflection;
using namespace System::Threading;
using namespace System::Diagnostics;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::DynamicInvocation;

Library::Library(FileInfo^ library)
{
	if(nullptr == library)
	{
		throw gcnew ArgumentNullException("library");
	}

	if (!library->Exists)
    {
        throw gcnew FileNotFoundException("The file was not found.", library->FullName);
    }

	if(!String::Equals(library->Extension, ".dll", StringComparison::OrdinalIgnoreCase))
	{
		throw gcnew ArgumentException(String::Format("The file {0} is not a valid library", library->Name));
	}

	m_Addresses = gcnew Dictionary<String^, ProcAddress^>();
	m_library = library;
	m_handle = nullptr;
}

Library::~Library()
{
	if(IsLoaded)
	{
		Unload();
	}
}

String^ 
Library::LibraryName::get()
{
	return m_library->Name;
}

DirectoryInfo^ 
Library::LibraryDirectory::get()
{
	return m_library->Directory;
}

bool 
Library::IsLoaded::get()
{
	return m_handle != nullptr;
}

HINSTANCE 
Library::Handle::get()
{
	if(!IsLoaded)
	{
		throw gcnew InvalidOperationException(String::Format("The Library {0} is not loaded", LibraryName));
	}
	
	return m_handle;
}

ProcAddress^
Library::GetProcAddressX(String^ methodName)
{
	if(String::IsNullOrEmpty(methodName))
	{
		throw gcnew ArgumentNullException("methodName");
	}

	Monitor::Enter(m_Addresses);
	marshal_context^ context = gcnew marshal_context();

	try
	{
		//Test wether we cached this one already
		if(m_Addresses->ContainsKey(methodName))
		{
			//We just return a copy here. This way we ensure that we have a proper reference counting
			ProcAddress^ address = m_Addresses[methodName];
			return gcnew ProcAddress(address);
		}

		const char* pName = context->marshal_as<const char*>(methodName);
		FARPROC procAddress = GetProcAddress(Handle, pName);

		if(nullptr == procAddress)
		{
			String^ err = GetErrorMessage(GetLastError());
			throw gcnew TargetException(err);
		}

		//Create an instance and store it in the dictionary
		ProcAddress^ address = gcnew ProcAddress(procAddress, methodName);
		m_Addresses->Add(address->ProcAddressName, address);

		//return a new copy of ProcAddress to ensure a proper reference counting
		return gcnew ProcAddress(address);
	}
	finally
	{
		delete context;
		Monitor::Exit(m_Addresses);
	}
}

void 
Library::Load()
{
	marshal_context^ context = gcnew marshal_context();

	try
	{
		const wchar_t* path = context->marshal_as<const wchar_t*>(LibraryDirectory->FullName);
		const wchar_t* lib = context->marshal_as<const wchar_t*>(LibraryName);

		if(false == SetDllDirectory(path))
		{
			String^ err = GetErrorMessage(GetLastError());
			throw gcnew FileLoadException(err);
		}

		m_handle = LoadLibrary(lib);
		if(nullptr == m_handle)
		{
			String^ err = GetErrorMessage(GetLastError());
			throw gcnew FileLoadException(err);
		}
	}
	finally
	{
		delete context;
	}
}

void 
Library::Unload()
{
	if(IsLoaded)
	{
		for each(ProcAddress^ address in m_Addresses->Values)
		{
			if(address->References > 1)
			{
				//There is at least one reference pending. Unable to unload the library
				String^ message = String::Format("The function {0} in module {1} is still referenced. Unable to unload the module", address->ProcAddressName, LibraryName);
				Debug::Fail(message);
				throw gcnew LibraryUnloadException(message);
			}
		}

		FreeLibrary(m_handle);
		
		m_handle = nullptr;
		m_Addresses->Clear();
	}
}

String^
Library::GetErrorMessage(int error)
{
	pin_ptr<wchar_t> buffer= new wchar_t[200]();
	FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM, nullptr, error, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), buffer, 200, nullptr);
	
	String^ message = gcnew String(buffer);
	LocalFree(buffer);
	
	return message;
}