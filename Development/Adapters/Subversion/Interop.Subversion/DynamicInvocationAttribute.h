#pragma once

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
							/// <summary>
							/// This attribute is used to mark methods that are using the dynamic invocation to laod specific methods
							/// </summary>
							[System::AttributeUsageAttribute(System::AttributeTargets::Method)]
							private ref class DynamicInvocationAttribute : public System::Attribute
							{
							private:
								System::String^ m_dllName;
								System::String^ m_entryPoint;

							public:
								/// <summary>
								/// Creates a new instance of the attribute
								/// </summary>
								/// <param name="dllName">The name of the dll that contains the requested method</param>
								/// <param name="entryPoint">The entrypoint (name) of the method that will be invoked</param>
								DynamicInvocationAttribute(System::String^ dllName, System::String^ entryPoint)
								{
									m_dllName = dllName;
									m_entryPoint = entryPoint;
								}

								/// <summary>
								/// Gets the name of the dll that contains the requested method
								/// </summary>
								property System::String^ DllName
								{
									System::String^ get()
									{
										return m_dllName;
									}
								}

								/// <summary>
								/// Gets the entrypoint (name) of the method that will be invoked
								/// </summary>	
								property System::String^ EntryPoint
								{
									System::String^ get()
									{
										return m_entryPoint;
									}
								}
							};
						}
					}
				}
			}
		}
	}
}