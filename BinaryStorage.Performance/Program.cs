using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BinStorage.Performance
{
    class Program
    {

        #region Properties

        [DllImport("kernel32.dll")]
        public static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);
               
        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);

        #endregion /Properties

        #region Methods

        public static void Main()
        {
            Console.WriteLine("------------Start---------------");

            // Define variables to track the peak
            // memory usage of the process.
            long peakPagedMem = 0,
                 peakWorkingSet = 0,
                 peakVirtualMem = 0;

            double maxPeakWorkingSet = Math.Pow(1024, 3); // 1GB
            string arguments = @"H:\Project\SmallData H:\Project\BinaryStorageFolder";            

            // Start the process.
            using (Process myProcess = Process.Start("BinStorage.TestApp.exe", arguments))
            {       
                try
                {
                    // Display the process statistics until
                    // the user closes the program.                
                    do
                    {
                        if (!myProcess.HasExited)
                        {
                            // Refresh the current process property values.
                            myProcess.Refresh();

                            Console.WriteLine();

                            // Display current process statistics.

                            Console.WriteLine($"{myProcess} -");
                            Console.WriteLine("-------------------------------------");

                            Console.WriteLine($"  Date & Time               : {DateTime.Now}");
                            Console.WriteLine($"  Physical memory usage     : {myProcess.WorkingSet64.ToString("#,0")} (bytes)");
                            Console.WriteLine($"  Base priority             : {myProcess.BasePriority}");
                            Console.WriteLine($"  Priority class            : {myProcess.PriorityClass}");
                            Console.WriteLine($"  User processor time       : {myProcess.UserProcessorTime}");
                            Console.WriteLine($"  Privileged processor time : {myProcess.PrivilegedProcessorTime}");
                            Console.WriteLine($"  Total processor time      : {myProcess.TotalProcessorTime}");
                            Console.WriteLine($"  Paged system memory size  : {myProcess.PagedSystemMemorySize64.ToString("#,0")}");
                            Console.WriteLine($"  Paged memory size         : {myProcess.PagedMemorySize64.ToString("#,0")}");

                            // Update the values for the overall peak memory statistics.
                            peakPagedMem = myProcess.PeakPagedMemorySize64;
                            peakVirtualMem = myProcess.PeakVirtualMemorySize64;
                            peakWorkingSet = myProcess.PeakWorkingSet64;

                            if (myProcess.Responding)
                            {
                                Console.WriteLine("Status = Running");
                            }
                            else
                            {
                                Console.WriteLine("Status = Not Responding");
                            }

                            if (peakWorkingSet > maxPeakWorkingSet)
                            {
                                SuspendProcess(myProcess);
                                FlushMemory(myProcess);
                                ResumeProcess(myProcess);
                            }
                        }
                    }
                    while (!myProcess.WaitForExit(1000));
                }
                catch (Exception ex)
                {

                }

                Console.WriteLine();
                Console.WriteLine($"  Process exit code          : {myProcess.ExitCode}");

                // Display peak memory statistics for the process.
                Console.WriteLine($"  Peak physical memory usage : {peakWorkingSet.ToString("#,0")} (bytes)");
                Console.WriteLine($"  Peak paged memory usage    : {peakPagedMem.ToString("#,0")} (bytes)");
                Console.WriteLine($"  Peak virtual memory usage  : {peakVirtualMem.ToString("#,0")} (bytes)");
                Console.ReadLine();
            }
        }

        private static void SuspendProcess(Process process)
        {
            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                SuspendThread(pOpenThread);
                CloseHandle(pOpenThread);
            }
        }

        public static void ResumeProcess(Process process)
        {
            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }
                var suspendCount = 0;
                do
                {
                    suspendCount = ResumeThread(pOpenThread);
                } while (suspendCount > 0);

                CloseHandle(pOpenThread);
            }
        }

        public static void FlushMemory(Process process)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                SetProcessWorkingSetSize(process.Handle, -1, -1);
            }
        }

        #endregion /Methods

    }
}
