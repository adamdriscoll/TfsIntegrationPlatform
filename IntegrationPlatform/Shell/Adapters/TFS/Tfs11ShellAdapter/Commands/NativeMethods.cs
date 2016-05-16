using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Shell.Tfs11ShellAdapter
{
    internal static class NativeMethods
    {
        internal const int ERROR_SUCCESS = 0;
        internal const uint CRED_TYPE_GENERIC = 0x1;
        internal const int NO_ERROR = 0;
        internal const int CREDUI_MAX_USERNAME_LENGTH = 513;
        internal const int CRED_PERSIST_LOCAL_MACHINE = 2;

        [StructLayout(LayoutKind.Sequential)]
        internal struct CREDENTIAL
        {
            public int Flags;
            public int Type;
            [MarshalAs(UnmanagedType.LPWStr)]
            public String TargetName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public String Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public int CredentialBlobSize;
            public IntPtr CredentialBlob;
            public int Persist;
            public int AttributeCount;
            public IntPtr Attributes;
            [MarshalAs(UnmanagedType.LPWStr)]
            public String TargetAlias;
            [MarshalAs(UnmanagedType.LPWStr)]
            public String UserName;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CREDENTIAL_ATTRIBUTE
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public String Keyword;
            public int Flags;
            public int ValueSize;
            public IntPtr Value;
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CredRead(String targetName,
                                           uint type,
                                           uint flags,
                                           out IntPtr credential);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CredWrite(ref NativeMethods.CREDENTIAL credential,
                                            uint flags);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CredDelete(String targetName,
                                             uint type,
                                             uint flags);

        [DllImport("advapi32.dll")]
        public static extern void CredFree(IntPtr buffer);


        [DllImport("credui.dll", CharSet = CharSet.Unicode)]
        public static extern int CredUIParseUserName(String pszUserName,
                                                     StringBuilder pszUser,
                                                     uint ulUserMaxChars,
                                                     StringBuilder pszDomain,
                                                     uint ulDomainMaxChars);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern void ZeroMemory(IntPtr address, uint byteCount);

    }
}
