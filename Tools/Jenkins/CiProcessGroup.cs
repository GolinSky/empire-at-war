using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

// Windows-only build tooling; compiled by PowerShell, never imported by Unity.
public sealed class CiProcessGroup : IDisposable
{
    private const uint CREATE_SUSPENDED = 4;
    private const uint CREATE_NO_WINDOW = 0x08000000;
    private const uint BELOW_NORMAL_PRIORITY_CLASS = 0x4000;
    private const uint KILL_ON_JOB_CLOSE = 0x2000;
    private const uint LIMIT_PRIORITY_CLASS = 0x20;
    private const uint WAIT_TIMEOUT = 258;
    private IntPtr _job;
    private IntPtr _process;

    public int ProcessId { get; private set; }
    public bool HasExited { get { return WaitForSingleObject(_process, 0) != WAIT_TIMEOUT; } }
    public uint ExitCode { get { uint code; Check(GetExitCodeProcess(_process, out code)); return code; } }

    public CiProcessGroup(string executable, string arguments, string directory, string log)
    {
        _job = CreateJobObject(IntPtr.Zero, null);
        Check(_job != IntPtr.Zero);
        IntPtr output = IntPtr.Zero;
        ProcessInformation info = new ProcessInformation();
        try
        {
            ExtendedLimits limits = new ExtendedLimits();
            limits.Basic.LimitFlags = KILL_ON_JOB_CLOSE | LIMIT_PRIORITY_CLASS;
            limits.Basic.PriorityClass = BELOW_NORMAL_PRIORITY_CLASS;
            Check(SetInformationJobObject(_job, 9, ref limits, (uint)Marshal.SizeOf(limits)));
            SecurityAttributes security = new SecurityAttributes();
            security.Length = Marshal.SizeOf(security);
            security.InheritHandle = true;
            output = CreateFile(log, 0x40000000, 3, ref security, 2, 0x80, IntPtr.Zero);
            Check(output != new IntPtr(-1));
            StartupInfo startup = new StartupInfo();
            startup.Size = Marshal.SizeOf(startup);
            startup.Flags = 0x100;
            startup.StandardOutput = output;
            startup.StandardError = output;
            Check(CreateProcess(executable, new StringBuilder("\"" + executable + "\" " + arguments), IntPtr.Zero, IntPtr.Zero, true,
                CREATE_SUSPENDED | CREATE_NO_WINDOW | BELOW_NORMAL_PRIORITY_CLASS, IntPtr.Zero, directory, ref startup, out info));
            _process = info.Process;
            ProcessId = (int)info.ProcessId;
            Check(AssignProcessToJobObject(_job, _process));
            bool contained;
            Check(IsProcessInJob(_process, _job, out contained));
            if (!contained) { throw new InvalidOperationException("CI launcher was not contained in its Job Object."); }
            Check(ResumeThread(info.Thread) != uint.MaxValue);
        }
        catch
        {
            if (info.Process != IntPtr.Zero) { TerminateProcess(info.Process, 1); }
            Dispose();
            throw;
        }
        finally
        {
            if (info.Thread != IntPtr.Zero) { CloseHandle(info.Thread); }
            if (output != IntPtr.Zero && output != new IntPtr(-1)) { CloseHandle(output); }
        }
    }

    public int[] GetProcessIds()
    {
        IntPtr buffer = Marshal.AllocHGlobal(65536);
        try
        {
            uint length;
            Check(QueryInformationJobObject(_job, 3, buffer, 65536, out length));
            int count = Marshal.ReadInt32(buffer, 4);
            int[] ids = new int[count];
            for (int i = 0; i < count; i++) { ids[i] = (int)Marshal.ReadIntPtr(buffer, 8 + i * IntPtr.Size); }
            return ids;
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    public void Dispose()
    {
        if (_job != IntPtr.Zero) { CloseHandle(_job); _job = IntPtr.Zero; }
        if (_process != IntPtr.Zero) { CloseHandle(_process); _process = IntPtr.Zero; }
    }

    private static void Check(bool success)
    {
        if (!success) { throw new Win32Exception(Marshal.GetLastWin32Error()); }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BasicLimits
    {
        public long ProcessTime, JobTime;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSet, MaximumWorkingSet;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass, SchedulingClass;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ExtendedLimits
    {
        public BasicLimits Basic;
        public ulong ReadOperations, WriteOperations, OtherOperations, ReadBytes, WriteBytes, OtherBytes;
        public UIntPtr ProcessMemoryLimit, JobMemoryLimit, PeakProcessMemory, PeakJobMemory;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct SecurityAttributes
    {
        public int Length;
        public IntPtr Descriptor;
        [MarshalAs(UnmanagedType.Bool)] public bool InheritHandle;
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct StartupInfo
    {
        public int Size;
        public string Reserved, Desktop, Title;
        public int X, Y, XSize, YSize, XCountChars, YCountChars, FillAttribute, Flags;
        public short ShowWindow, ReservedSize;
        public IntPtr ReservedData, StandardInput, StandardOutput, StandardError;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        public IntPtr Process, Thread;
        public uint ProcessId, ThreadId;
    }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateJobObject(IntPtr attributes, string name);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(IntPtr job, int type, ref ExtendedLimits limits, uint length);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool QueryInformationJobObject(IntPtr job, int type, IntPtr buffer, uint length, out uint returned);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateProcess(string application, StringBuilder command, IntPtr processAttributes, IntPtr threadAttributes,
        bool inherit, uint flags, IntPtr environment, string directory, ref StartupInfo startup, out ProcessInformation process);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateFile(string path, uint access, uint share, ref SecurityAttributes attributes, uint disposition, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool IsProcessInJob(IntPtr process, IntPtr job, out bool contained);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint ResumeThread(IntPtr thread);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetExitCodeProcess(IntPtr process, out uint code);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool TerminateProcess(IntPtr process, uint code);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
}
