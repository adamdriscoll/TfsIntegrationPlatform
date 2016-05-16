#include "stdafx.h"
#include "LibraryLoader.h"
#include "SubversionNotFoundException.h"

using namespace System::Diagnostics;
using namespace System::Threading;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::DynamicInvocation;

LibraryLoader::LibraryLoader()
{
	m_loadedLibs = gcnew Dictionary<String^, Library^>(StringComparer::OrdinalIgnoreCase);
	m_SubversionInstallationDirectory = nullptr;
}

LibraryLoader::~LibraryLoader()
{
	ReleaseLibraries();
}

LibraryLoader^
LibraryLoader::Instance()
{
	return m_loader;
}

Library^ 
LibraryLoader::GetLibrary(String^ name)
{
	if(nullptr == name)
	{
		throw gcnew ArgumentNullException("name");
	}

	Library^ library;
	Monitor::Enter(m_loadedLibs);
	try
	{
		//No we have to check wether we loaded it alredy. If this is the case, we just return that refrence
		name = GetNormalizedLibraryString(name);
		if(m_loadedLibs->ContainsKey(name))
		{
			return m_loadedLibs[name];
		}

		String^ installationDir = GetSubversionInstallationDirectory();
		FileInfo^ libraryFile = gcnew FileInfo(Path::Combine(installationDir, name));

		if(!libraryFile->Exists)
		{
			//The library file does not exist and hence no valid subversion installation available
			throw gcnew SubversionNotFoundException();
		}

		//The library has not yet been loaded.
		library = gcnew Library(libraryFile);
		library->Load();

		m_loadedLibs->Add(library->LibraryName, library);
	}
	finally
	{
		Monitor::Exit(m_loadedLibs);
	}

	return library;
}

ProcAddress^
LibraryLoader::GetProcAddress(String^ library, String^ method)
{
	//This method either succeeds or throws an exception. Therefore we do not have to check for null values
	return GetLibrary(library)->GetProcAddressX(method);
}

ProcAddress^ 
LibraryLoader::GetProcAddress(MethodBase^ method)
{
	if(nullptr == method)
	{
		throw gcnew ArgumentNullException("method");
	}

	array<Object^>^ attr = method->GetCustomAttributes(DynamicInvocationAttribute::typeid,false);
	if(nullptr == attr || attr->Length == 0)
	{
		String^ message = String::Format("The method {0} does not have the required T(DynamicInvocationAttribute)", method->Name);
		Debug::Fail(message);
		throw gcnew InvalidOperationException(message);
	}

	DynamicInvocationAttribute^ extractedAttr = safe_cast<DynamicInvocationAttribute^> (attr[0]);
	return GetProcAddress(extractedAttr->DllName, extractedAttr->EntryPoint);
}

void
LibraryLoader::ReleaseLibrary(Library^ library)
{
	if(nullptr == library)
	{
		return;
	}

	ReleaseLibrary(library->LibraryName);
}

void 
LibraryLoader::ReleaseLibrary(String^ name)
{
	name = GetNormalizedLibraryString(name);

	Monitor::Enter(m_loadedLibs);
	try
	{
		if(m_loadedLibs->ContainsKey(name))
		{
			Library^ lib = m_loadedLibs[name];
			lib->Unload();
			m_loadedLibs->Remove(name);
		}
	}
	finally
	{
		Monitor::Exit(m_loadedLibs);
	}
}

void 
LibraryLoader::ReleaseLibraries()
{
	Monitor::Enter(m_loadedLibs);
	try
	{
		for each(Library^ library in m_loadedLibs->Values)
		{
			library->Unload();
		}

		m_loadedLibs->Clear();
	}
	finally
	{
		Monitor::Exit(m_loadedLibs);
	}
}

String^ 
LibraryLoader::GetNormalizedLibraryString(String^ name)
{
	if(String::IsNullOrEmpty(name))
	{
		return name;
	}

	//Test wether the string ends on .dll. Attach .dll if not
	if(!name->EndsWith(".dll", StringComparison::OrdinalIgnoreCase))
	{
		name = name + ".dll";
	}

	return name;
}

String^ 
LibraryLoader::GetSubversionInstallationDirectory()
{
	//TODO: Offer the possibility to overwrite the installation directory using a configuration file
	if(nullptr != m_SubversionInstallationDirectory)
	{
		return m_SubversionInstallationDirectory;
	}

	//First of all we are going to check the default installation directory of svn
	String^ programFiles = Environment::GetFolderPath(Environment::SpecialFolder::ProgramFiles);
	String^ binaryDir = Path::Combine(Path::Combine(programFiles, "svn"), "bin");
	if(IsSubversionInstallationDirectory(binaryDir))
	{
		m_SubversionInstallationDirectory = binaryDir;
		return m_SubversionInstallationDirectory;
	}

	//Most svn installation add themself into the path variable. 
	//Therefore we can probe the path variables and try to get the installation directory

	//We have to undefine the c version of GetEnvironmentVariable in order to use the managed version.
	#ifdef GetEnvironmentVariable
	#undef GetEnvironmentVariable
	#endif

	String^ pathVariable = System::Environment::GetEnvironmentVariable("path");
	if(!String::IsNullOrEmpty(pathVariable))
	{
		for each (String^ path in pathVariable->Split(';'))
		{
			if(IsSubversionInstallationDirectory(path))
			{
				m_SubversionInstallationDirectory = path;
				return m_SubversionInstallationDirectory;
			}
		}
	}

	//Unable to determine the subversion installation directory
	throw gcnew SubversionNotFoundException();
}

bool 
LibraryLoader::IsSubversionInstallationDirectory(String^ dir)
{
	if(nullptr == dir || !Directory::Exists(dir))
	{
		return false;
	}

	//We are just testing wether the directory contains the "svn.exe" file. Its the subversion installation directory if it is there
	String^ fullpath = Path::Combine(dir, "svn.exe");
	return File::Exists(fullpath);
}
