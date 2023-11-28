#include "pch.h"
#include <sddl.h>
#include <accctrl.h>
#include <aclapi.h>
#include <memory>

#pragma comment(lib, "advapi32.lib")

#include "Win32APILib.h"
using namespace Win32APILib;

struct HandleDeleter {
    typedef HANDLE pointer;
    void operator ()(HANDLE handle) const
    {
        if (handle != INVALID_HANDLE_VALUE) {
            CloseHandle(handle);
        }
    }
};
struct LocalDeleter {
    typedef HLOCAL pointer;
    void operator ()(HLOCAL ptr) const
    {
        if (ptr != nullptr) {
            LocalFree(ptr);
        }
    }
};

typedef std::unique_ptr<HANDLE, HandleDeleter> unique_handle;
typedef std::unique_ptr<HANDLE, LocalDeleter> unique_local;

bool FileUtility::PipeExists(String^ pipeName)
{
    const CStringW pipeNameW(pipeName);

    const auto hPipe = CreateFileW(pipeNameW, GENERIC_READ | GENERIC_WRITE, 0, nullptr, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, nullptr);
    unique_handle hPipeDeleter(hPipe);

    if (hPipe == INVALID_HANDLE_VALUE)
    {
        const auto win32Error = GetLastError();
        if (win32Error == ERROR_PIPE_BUSY)
        {
            return true;
        }
        else
        {
            return false;
        }
    }


    return true;

}

void FileUtility::PipeSetAclAuthenticatedUsersPermit(String^ pipeName)
{
    const CStringW pipeNameW(pipeName);

    HANDLE hPipe = INVALID_HANDLE_VALUE;
    unique_handle hPipeDeleter(hPipe);
    {
        hPipe = CreateFileW(pipeNameW, GENERIC_READ | GENERIC_WRITE | WRITE_DAC, 0, nullptr, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, nullptr);

        if (hPipe == INVALID_HANDLE_VALUE)
        {
            const auto win32Error = GetLastError();
            throw gcnew Exception(String::Format("CreateNamedPipe failed with error 0x{0:X}", win32Error));
        }
    }

    PSID pSIDUser = nullptr;
    unique_local pSIDUserDeleter(pSIDUser);

    if (!ConvertStringSidToSidW(L"S-1-5-11", &pSIDUser)) {
        const auto win32Error = GetLastError();
        throw gcnew Exception(String::Format("ConvertStringSidToSid failed with error 0x{0:X}", win32Error));
    }

    EXPLICIT_ACCESSW eaAccess = {};
    eaAccess.grfAccessPermissions = GENERIC_ALL;
    eaAccess.grfAccessMode = SET_ACCESS;
    eaAccess.grfInheritance = SUB_CONTAINERS_AND_OBJECTS_INHERIT;
    eaAccess.Trustee.TrusteeForm = TRUSTEE_IS_SID;
    eaAccess.Trustee.TrusteeType = TRUSTEE_IS_USER;
    eaAccess.Trustee.ptstrName = static_cast<LPWSTR>(pSIDUser);

    PSECURITY_DESCRIPTOR pSD = nullptr;
    unique_local pSDDeleter(pSD);

    PACL pOldDACL = nullptr;
    {
        const auto ret = GetSecurityInfo(hPipe, SE_KERNEL_OBJECT, DACL_SECURITY_INFORMATION, nullptr, nullptr, &pOldDACL, nullptr, &pSD);
        if (ret != ERROR_SUCCESS)
        {
            throw gcnew Exception(String::Format("GetSecurityInfo failed with error 0x{0:X}", ret));
        }
    }

    PACL pNewDACL = nullptr;
    unique_local pNewDACLDeleter(pNewDACL);
    {
        const auto ret = SetEntriesInAclW(1, &eaAccess, pOldDACL, &pNewDACL);
        if (ret != ERROR_SUCCESS) {
            throw gcnew Exception(String::Format("SetEntriesInAcl failed with error 0x{0:X}", ret));
        }
    }

    {
        const auto ret = SetSecurityInfo(hPipe, SE_KERNEL_OBJECT, DACL_SECURITY_INFORMATION, nullptr, nullptr, pNewDACL, nullptr);
        if (ret != ERROR_SUCCESS) {
            throw gcnew Exception(String::Format("SetSecurityInfo failed with error 0x{0:X}", ret));
        }
    }
	
}


