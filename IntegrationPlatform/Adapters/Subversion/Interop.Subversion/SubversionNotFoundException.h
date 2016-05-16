using namespace System::Runtime::Serialization;

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
							public ref class SubversionNotFoundException : Exception
							{
							private:
								static String^ msg = "Unable to locate the local subversion installation which is required by this adapter. Please install a local subversion installation like 'http://sourceforge.net/projects/win32svn/' on your system";

							public:
								SubversionNotFoundException() : Exception(msg) { }
        
								SubversionNotFoundException(String^ message) : Exception(message) { }
        
								SubversionNotFoundException(String^ message, Exception^ innerException) : Exception(message, innerException) { }
							};
						}
					}
				}
			}
		}
	}
}