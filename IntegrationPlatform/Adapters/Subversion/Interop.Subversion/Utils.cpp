#include "stdafx.h"
#include "Utils.h"

using namespace System;
using namespace Microsoft::TeamFoundation::Migration::SubversionAdapter::Interop::Subversion::Helpers;

String^
Utils::Combine(String^ root, String^ relative)
{
	if(nullptr == root)
	{
		throw gcnew ArgumentNullException("root");
	}

	if(String::IsNullOrEmpty(relative))
	{
		return root;
	}

	String^ rootString = root->TrimEnd(SeperatorCharArray);
	relative = relative->Trim(SeperatorCharArray);
	
	return String::Concat(rootString, Seperator, relative);
}

String^
Utils::ExtractPath(String^ baseUri, String^ fullUri)
{
    if (nullptr == baseUri)
    {
        throw gcnew ArgumentNullException("baseUri");
    }

    if (nullptr == fullUri)
    {
        throw gcnew ArgumentNullException("fullUri");
    }

	String^ baseUriString = baseUri->ToString()->TrimEnd(SeperatorCharArray);
	String^ fullUriString = fullUri->ToString()->TrimEnd(SeperatorCharArray);

	// Check whether base and the full uri are the same. In this case this is root. Hence we return the root sign
	if(baseUriString->Length == fullUriString->Length && baseUriString->Equals(fullUriString, StringComparison::OrdinalIgnoreCase))
	{
		return Seperator;
	}

	if (baseUriString->Length > fullUriString->Length)
    {
		//The base uri cannot be longer than the uri from which we want to extract the path because this item must be a subitem of the root item
		String^ message = String::Format("The item '{0}' is not child of '{1}'. It is not possible to convert it to a path item in the repository", fullUri, baseUri);
        throw gcnew FormatException(message);
    }

    if (!fullUriString->StartsWith(baseUriString + Seperator))
    {
		//We can only extract the path of items which are within the current scope.
        String^ message = String::Format("The item '{0}' is not child of '{1}'. It is not possible to convert it to a path item in the repository", fullUri, baseUri);
        throw gcnew FormatException(message);
    }

    return fullUriString->Substring(baseUriString->Length);
}

String^ 
Utils::ConvertUTF8ToString(const char* value)
{
	if(NULL == value)
	{
		return nullptr;
	}

	return gcnew String(value, 0, strlen(value), System::Text::Encoding::UTF8);
}
