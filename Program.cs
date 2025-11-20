using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

class Program
{
    // ----------------------------
    // Win32 API for driver communication
    // ----------------------------
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        IntPtr hDevice,
        uint dwIoControlCode,
        byte[] lpInBuffer,
        uint nInBufferSize,
        byte[] lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    // ----------------------------
    // IOCTL for hiding process
    // ----------------------------
    const uint FILE_DEVICE_UNKNOWN = 0x00000022;
    const uint METHOD_BUFFERED = 0;
    const uint FILE_ANY_ACCESS = 0;
    const uint IOCTL_HIDE_PROCESS_BY_PID = (FILE_DEVICE_UNKNOWN << 16) | (0x800 << 2) | METHOD_BUFFERED | (FILE_ANY_ACCESS << 14);

    // ----------------------------
    static void Main()
    {
        Console.Title = "ResidentPCBApp";
        Console.WriteLine("ResidentPCBApp started at: " + DateTime.Now);

        // Example: monitor self or a child process
        Process process = Process.GetCurrentProcess();

        // ----------------------------
        // Print PCB info
        // ----------------------------
        PrintProcessInfo(process);

        // ----------------------------
        // Call driver to hide this process
        // ----------------------------
        HideProcessWithDriver(process.Id);

        Console.WriteLine("\nPress Ctrl+C to exit.");
        Thread.Sleep(Timeout.Infinite);
    }

    // ----------------------------
    static void PrintProcessInfo(Process p)
    {
        try
        {
           // BASIC INFO
            PCB.Add(new ProcessInfo("PID", _proc.Id.ToString()));
            PCB.Add(new ProcessInfo("Name", _proc.ProcessName));
            PCB.Add(new ProcessInfo("Start Time", Safe(() => _proc.StartTime.ToString())));
            PCB.Add(new ProcessInfo("Responding", _proc.Responding.ToString()));

            // PRIORITY & THREADS
            PCB.Add(new ProcessInfo("Base Priority", _proc.BasePriority.ToString()));
            PCB.Add(new ProcessInfo("Priority Class", Safe(() => _proc.PriorityClass.ToString())));
            PCB.Add(new ProcessInfo("Thread Count", _proc.Threads.Count.ToString()));

            // MEMORY
            PCB.Add(new ProcessInfo("Working Set", $"{_proc.WorkingSet64 / 1024} KB"));
            PCB.Add(new ProcessInfo("Peak Working Set", $"{_proc.PeakWorkingSet64 / 1024} KB"));
            PCB.Add(new ProcessInfo("Private Memory", $"{_proc.PrivateMemorySize64 / 1024} KB"));
            PCB.Add(new ProcessInfo("Virtual Memory", $"{_proc.VirtualMemorySize64 / 1024} KB"));
            PCB.Add(new ProcessInfo("Paged Memory", $"{_proc.PagedMemorySize64 / 1024} KB"));
            PCB.Add(new ProcessInfo("Nonpaged System Memory", $"{_proc.NonpagedSystemMemorySize64 / 1024} KB"));
            PCB.Add(new ProcessInfo("Paged System Memory", $"{_proc.PagedSystemMemorySize64 / 1024} KB"));

            // CPU TIME
            PCB.Add(new ProcessInfo("User CPU Time", Safe(() => _proc.UserProcessorTime.ToString())));
            PCB.Add(new ProcessInfo("Kernel CPU Time", Safe(() => _proc.PrivilegedProcessorTime.ToString())));
            PCB.Add(new ProcessInfo("Total CPU Time", Safe(() => _proc.TotalProcessorTime.ToString())));

            // HANDLES & MODULES
            PCB.Add(new ProcessInfo("Handle Count", _proc.HandleCount.ToString()));
            PCB.Add(new ProcessInfo("Module Count", Safe(() => _proc.Modules.Count.ToString())));

            // NETWORK / SESSION
            PCB.Add(new ProcessInfo("Session ID", _proc.SessionId.ToString()));

            // MACHINE INFO
            PCB.Add(new ProcessInfo("Machine Name", _proc.MachineName));
            PCB.Add(new ProcessInfo("Main Window Title", _proc.MainWindowTitle));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error retrieving process info: " + ex.Message);
        }
    }

    // ----------------------------
    static void HideProcessWithDriver(int pid)
    {
        string driverPath = @"\\.\MyKernelDriverDKOM";

        IntPtr hDevice = CreateFile(driverPath,
            0xC0000000, // GENERIC_READ | GENERIC_WRITE
            0x00000001 | 0x00000002, // FILE_SHARE_READ | FILE_SHARE_WRITE
            IntPtr.Zero,
            3, // OPEN_EXISTING
            0,
            IntPtr.Zero);

        if (hDevice == new IntPtr(-1))
        {
            Console.WriteLine("Failed to open driver. Make sure it is installed and running.");
            return;
        }

        byte[] inBuffer = BitConverter.GetBytes(pid);
        byte[] outBuffer = new byte[1024];
        uint bytesReturned;

        bool success = DeviceIoControl(
            hDevice,
            IOCTL_HIDE_PROCESS_BY_PID,
            inBuffer,
            (uint)inBuffer.Length,
            outBuffer,
            (uint)outBuffer.Length,
            out bytesReturned,
            IntPtr.Zero);

        if (success)
            Console.WriteLine($"[Driver] Request to hide PID {pid} sent successfully.");
        else
            Console.WriteLine($"[Driver] Failed to send IOCTL. Error: {Marshal.GetLastWin32Error()}");

        CloseHandle(hDevice);
    }
}