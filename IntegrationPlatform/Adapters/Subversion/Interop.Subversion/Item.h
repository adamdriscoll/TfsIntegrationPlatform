#pragma once

#include <svn_pools.h>
#include "svn_client.h"
#include <msclr/marshal.h>

using namespace System;
using namespace System::Collections::Generic;
using namespace msclr::interop;

using namespace Microsoft::TeamFoundation::Migration::Toolkit::Services;

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
						namespace ObjectModel
						{
							public ref class Item
							{
							private:
								String^ m_fullServerPath;
								String^ m_path;
								String^ m_repositoryRoot;

								String^ m_lastAuthor;
								long m_createdRev;

								ContentType^ m_itemType;
								long m_size;								

							internal:
								/// <summary>
								/// Default Constructor
								/// </summary>
								/// <param name"fullpath">The full path to the item</param>
								/// <param name"dirent">The dirent object that contains additional attributes</param>
								/// <param name"repositoryRoot">The root of the repository that will be used to calculate relative paths</param>
								Item(String^ fullpath, const svn_dirent_t* dirent, String^ repositoryRoot);	

							public:
								
								/// <summary>
								/// Gets the content / item type of this file or folder
								/// </summary>
								property ContentType^ ItemType { ContentType^ get(); }

								/// <summary>
								/// length of file text, or 0 for directories
								/// </summary>
								property long Size { long get(); }

								/// <summary>
								/// The author who changed the item last
								/// </summary>
								property String^ LastAuthor { String^ get(); }

								/// <summary>
								/// Gets the revision when the item has been created
								/// </summary>
								property long CreatedRev { long get(); }

								/// <summary>
								/// Gets the path of the repository
								/// <para/>
								/// Example Value: svn://localhost/repos/Svn2Tfs
								/// </summary>
								property String^ Repository { String^ get(); }

								/// <summary>
								/// Gets the absolute <see cref="Uri"/> of the item in the svn repository.
								/// The <see cref="Uri"/> object also contains the url to the svn repository
								/// <para/>
								/// Example Value: svn://localhost/repos/Svn2Tfs/Folder/File.txt
								/// </summary>
								property String^ FullServerPath { String^ get(); }

								/// <summary>
								/// Gets the relative <see cref="Uri"/> of the item within the repository.
								/// The <see cref="Uri"/> object does not contain the repository url. 
								/// It just contains the path part of the item in the repository.
								/// The Path always starts with an slash
								/// <para/>
								/// Example Value: /Folder/File.txt
								/// </summary>
								property String^ Path {String^ get(); }

								//This item has a couple more properties which are not exported. They may have to be exported later if needed
							};
						}
					}
				}
			}
		}
	}
}