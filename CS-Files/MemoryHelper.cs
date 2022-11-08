using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Windows_Startup_Cleaner
{
    public class CleanMemory
    {
        [DllImport("advapi32.dll", SetLastError = true)] internal static extern bool LookupPrivilegeValue(string host, string name, ref long pluid);
        [DllImport("advapi32.dll", SetLastError = true)] internal static extern bool AdjustTokenPrivileges(IntPtr htok, bool disall, ref TokPriv1Luid newst, int len, IntPtr prev, IntPtr relen);
        [DllImport("ntdll.dll")] private static extern uint NtSetSystemInformation(int InfoClass, IntPtr Info, int Length);
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SYSTEM_CACHE_INFORMATION
        {
            public long CurrentSize;
            public long PeakSize;
            public long PageFaultCount;
            public long MinimumWorkingSet;
            public long MaximumWorkingSet;
            public long Unused1;
            public long Unused2;
            public long Unused3;
            public long Unused4;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)] internal struct TokPriv1Luid { public int Count; public long Luid; public int Attr; }

        public void ClearCache()
        {
            try
            {
                if (SetIncreasePrivilege("SeIncreaseQuotaPrivilege"))
                {
                    SYSTEM_CACHE_INFORMATION sc = new SYSTEM_CACHE_INFORMATION
                    { MinimumWorkingSet = Environment.Is64BitOperatingSystem ? -1L : uint.MaxValue, MaximumWorkingSet = Environment.Is64BitOperatingSystem ? -1L : uint.MaxValue };
                    int sys = Marshal.SizeOf(sc);
                    GCHandle gcHandle = GCHandle.Alloc(sc, GCHandleType.Pinned);
                    uint num = NtSetSystemInformation(0x0015, gcHandle.AddrOfPinnedObject(), sys);
                    gcHandle.Free();
                }

                if (SetIncreasePrivilege("SeProfileSingleProcessPrivilege"))
                {
                    int sys = Marshal.SizeOf(4);
                    GCHandle gcHandle = GCHandle.Alloc(4, GCHandleType.Pinned);
                    uint num = NtSetSystemInformation(0x0050, gcHandle.AddrOfPinnedObject(), sys);
                    gcHandle.Free();
                }
            }
            catch { }
        }

        private bool SetIncreasePrivilege(string privilegeName)
        {
            using (WindowsIdentity current = WindowsIdentity.GetCurrent(TokenAccessLevels.Query | TokenAccessLevels.AdjustPrivileges))
            {
                TokPriv1Luid tokPriv1Luid;
                tokPriv1Luid.Count = 1;
                tokPriv1Luid.Luid = 0L;
                tokPriv1Luid.Attr = 2;
                return LookupPrivilegeValue(null, privilegeName, ref tokPriv1Luid.Luid) && AdjustTokenPrivileges(current.Token, false, ref tokPriv1Luid, 0, IntPtr.Zero, IntPtr.Zero);
            }
        }
    }
}