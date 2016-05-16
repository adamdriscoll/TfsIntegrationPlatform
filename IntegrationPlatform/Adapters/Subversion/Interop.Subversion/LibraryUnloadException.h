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
							public ref class LibraryUnloadException : Exception
							{
							public:
								LibraryUnloadException() { }
        
								LibraryUnloadException(String^ message) : Exception(message) { }
        
								LibraryUnloadException(String^ message, Exception^ innerException) : Exception(message, innerException) { }
							};
						}
					}
				}
			}
		}
	}
}