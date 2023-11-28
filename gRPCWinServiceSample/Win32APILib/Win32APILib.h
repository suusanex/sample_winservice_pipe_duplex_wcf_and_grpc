#pragma once

using namespace System;

namespace Win32APILib {
	public ref class FileUtility
	{
	public:
		static bool PipeExists(String^ pipeName);
		static void PipeSetAclAuthenticatedUsersPermit(String^ pipeName);
	};
}
