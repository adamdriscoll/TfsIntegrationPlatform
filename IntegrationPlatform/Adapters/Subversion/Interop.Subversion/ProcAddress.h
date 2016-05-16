#pragma once

#include <Windows.h>
#include <Winbase.h>

using namespace System;

namespace Microsoft
{
	namespace TeamFoundation
	{
		namespace Migration
		{
			namespace SubversionAdapter
			{
				namespace Interop
				{
					namespace Subversion
					{
						namespace DynamicInvocation
						{
							private ref class ProcAddress
							{
							private:
								//The actual address of the mthod
								FARPROC m_procAddress;

								//The name of the method
								String^ m_procAddressName;

								//Reference counting
								int^ m_referenceCount;

							public:
								/// <summary>
								/// Initializes a new instance of the proc address object
								/// </summary>
								/// <param name="procAddress">The actual procaddress handle</param>
								/// <param name="procAddressName">The name of the method</param>
								ProcAddress(FARPROC procAddress, String^ procAddressName);

								/// <summary>
								/// Copy constructor
								/// </summary>
								/// <param name="address">The object that has to be copied</param>
								ProcAddress(ProcAddress^ address);

								/// <summary>
								/// Default destructor
								/// </summary>
								~ProcAddress();

								/// <summary>
								/// Gets the proc handle
								/// </summary>
								property FARPROC Handle
								{
									FARPROC get();
								}

								/// <summary>
								/// Gets the proc name
								/// </summary>
								property String^ ProcAddressName
								{
									String^ get();
								}

								/// <summary>
								/// The amount of references to the actual ProcAddress handle
								/// </summary>
								property int References
								{
									int get();
								}
							};
						}
					}
				}
			}
		}
	}
}
