using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BinStorage.Common
{
    /// <summary>
    /// This class is used for forcing Garbage Collector to delete unused objects from the heap 
    /// in order to free up the memory
    /// </summary>
    public class MemoryManager
    {

        #region Properties

        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

        private readonly int _memoryCriticalSize;

        private readonly int _memoryFlushSizeLimit;

        private bool _isMemoryFlushing = false;

        private static readonly object locker = new object();

        #endregion /Properties

        #region Consructors

        public MemoryManager()
        {
            _memoryCriticalSize = int.Parse(Utility.GetAppSetting(Constant.AppSetting_MemoryCriticalSizeLimitInKB)) * 1024;
            _memoryFlushSizeLimit = int.Parse(Utility.GetAppSetting(Constant.AppSetting_MemoryFlushSizeLimitInKB)) * 1024;
        }

        #endregion /Consructors

        #region Methods        

        public void OptimizeMemoryConsumption()
        {
            if (!_isMemoryFlushing && IsFlushMemoryNeeded())
            {
                _isMemoryFlushing = true;
                lock (locker) // to prevent multi threads from flushing the memory at the same time
                {
                    if (IsFlushMemoryNeeded())
                    {
                        Utility.ConsoleWriteLine("Flushing the memory");
                        FlushMemory();
                        while (IsMemoryCritical()) // Just wait for the memory to be freed up to prevent the run out of memory
                        { /// maybe not a good solution, but better than having an OutOfMemory exception
                            Utility.ConsoleWriteLine("Critical Memory");
                            FlushMemory();
                        }
                    }
                }
                _isMemoryFlushing = false;
            }
        }

        private bool IsMemoryCritical()
        {
            return (GC.GetTotalMemory(false) > _memoryCriticalSize);
        }

        private bool IsFlushMemoryNeeded()
        {
            return (GC.GetTotalMemory(false) > _memoryFlushSizeLimit);
        }

        public void FlushMemory()
        {
            //GC.Collect(2, GCCollectionMode.Forced); 
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect(); // call for the second time to collect the finalized objects
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            { /// acording to my tests, it was useful for reducing memory usage
                SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
            }
        }

        #endregion /Methods

    }
}
