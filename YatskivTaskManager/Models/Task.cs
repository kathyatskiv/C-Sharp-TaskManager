using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic.Devices;

namespace YatskivTaskManager.Models
{
    class Task : INotifyPropertyChanged
    {
        #region  Fields
        private readonly Process _task;
        private readonly string _path;
        private readonly DateTime _time;

        private float _curcpu;
        private float _curram;
        PerformanceCounter _cpucounter;
        PerformanceCounter _ramcounter;

        private readonly double _total;
        #endregion

        #region Constructor
        public Task(Process task)
        {
            _task = task;
            _total = (new ComputerInfo()).TotalPhysicalMemory;
            _ramcounter = new PerformanceCounter("Process", "Working Set", Name);
            _cpucounter = new PerformanceCounter("Process", "% Processor Time", Name);

            try
            {
                _time = _task.StartTime;
                _path = _task.MainModule.FileName;
            }
            catch (Exception) {}
        }
        #endregion

        #region Properties
        public string Name { get { return _task.ProcessName; } }

        public int ID { get { return _task.Id; } }
        
        public bool IsActive { get { return _task.Responding; } }

        public string CPU { get { return ((float)Math.Round(_curcpu * 100f) / 100f).ToString("0.00") + "%"; } }
        
        public string RAM { get { return ((_curram / _total) * 100).ToString("0.00") + "% , " + (_curram / (1024 * 1024)).ToString("0.00") + "MB"; } }

        public int ThreadsAmount { get { return _task.Threads.Count; } }
        
        public string UserName { get { return GetTaskOwner(ID); } }
        
        public string FileInfo { get { return _path; } }
        
        public DateTime Time { get { return _time; } }

        public string FilePath { get { return _path; } }

        public ProcessThreadCollection Threads
        {
            get
            {
                try
                {
                    ProcessThreadCollection ptc = _task.Threads;
                    return _task.Threads;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public ProcessModuleCollection Modules
        {
            get
            {
                try
                {
                    ProcessModuleCollection pmc = _task.Modules;
                    return _task.Modules;
                }
                catch (Exception e)
                {
                    return null;
                }
            }
        }

        #endregion

        #region Methods

        public void UpdateTask()
        {
            try
            {
                _curcpu = _cpucounter.NextValue() / Environment.ProcessorCount;
                _curram = _ramcounter.NextValue();
            }
            catch (Exception) {}

            OnPropertyChanged("RAM");
            OnPropertyChanged("CPU");
            OnPropertyChanged("ThreadsQuantity");
        }

        public string GetTaskOwner(int processId)
        {
            string search = "Select * From Win32_Process Where ProcessID = " + processId;
            ManagementObjectSearcher searchres = new ManagementObjectSearcher(search);
            ManagementObjectCollection processList = searchres.Get();

            foreach (ManagementObject el in processList)
            {
                string[] argList = new string[] { string.Empty, string.Empty };
                int owner = Convert.ToInt32(el.InvokeMethod("GetOwner", argList));

                if (owner == 0) return argList[1] + "\\" + argList[0];
            }

            return "NO OWNER";
        }

        public void KillTask() { _task.Kill(); }

        #endregion

        #region OnPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
