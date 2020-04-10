using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Data;
using YatskivTaskManager.Tools;
using YatskivTaskManager.Models;

namespace YatskivTaskManager.ViewModels
{
    internal class MainWindowViewModel : INotifyPropertyChanged
    {
        #region Fields

        private ConcurrentDictionary<int, Task> _tasksList = new ConcurrentDictionary<int, Task>();
        private KeyValuePair<int, Task> _selected;

        private CollectionViewSource _viewSource = new CollectionViewSource();

        private Thread _workingThread;
        private Thread _metaDatesThread;

        private CancellationToken _token;
        private CancellationTokenSource _tokenSource;

        private RelayCommand<object> _openFolderCommand;
        private RelayCommand<object> _killTaskCommand;
        #endregion

        #region Constructor

        public MainWindowViewModel()
        {
            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;

            Synchronize();
            StartWorkingThread();
            StationManager.KillThreads += StopWorkingThread;
            ViewSource.Source = _tasksList;
        }
        #endregion

        #region Properties

        public KeyValuePair<int, Task> SelectedProcess
        {
            get { return _selected; }
            set
            {
                _selected = value;
                OnPropertyChanged();
                OnPropertyChanged("ProcessModules");
                OnPropertyChanged("ProcessThreads");
            }
        }

        public CollectionViewSource ViewSource
        {
            get
            {
                KeyValuePair<int, Task> p = _selected;
                _viewSource?.View?.Refresh();
                SelectedProcess = p;
                return _viewSource;
            }
        }

        public ProcessModuleCollection ProcessModules { get { return SelectedProcess.Value?.Modules; } }

        public ProcessThreadCollection ProcessThreads { get { return SelectedProcess.Value?.Threads; } }
        #endregion

        #region Commands
        public RelayCommand<object> Stop => _killTaskCommand ?? (_killTaskCommand = new RelayCommand<object>(
                                                o => KillTask(), o => CanExecute()));

        private void KillTask()
        {
            SelectedProcess.Value.KillTask();
            OnPropertyChanged("ViewSource");
        }

        public RelayCommand<object> OpenFolder => _openFolderCommand ?? (_openFolderCommand = new RelayCommand<object>(
                                                      o => Open(), o => CanExecuteFolder()));

        private void Open()
        {
            Process.Start("explorer.exe", "/select, \"" + SelectedProcess.Value.FilePath + "\"");
        }

        private bool CanExecute() => SelectedProcess.Value != null;

        private bool CanExecuteFolder() => SelectedProcess.Value?.FilePath != null && SelectedProcess.Value != null;

        #endregion

        #region Functions

        private void Synchronize()
        {
            Process[] tasks = Process.GetProcesses();
            var previous = new HashSet<int>(_tasksList.Keys);

            foreach (var task in tasks)
            {
                _tasksList.GetOrAdd(task.Id, new Task(task));
                previous.Remove(task.Id);

                if (_token.IsCancellationRequested) return;
            }

            foreach (var task in previous)
            {
                Task t;
                _tasksList.TryRemove(task, out t);

                if (_token.IsCancellationRequested) return;
            }

            OnPropertyChanged("ViewSource");
        }

        private void StartWorkingThread()
        {
            _workingThread = new Thread(WorkingThreadTask);
            _workingThread.Start();

            _metaDatesThread = new Thread(UpdateMetaDates);
            _metaDatesThread.Start();
        }

        internal void StopWorkingThread()
        {
            _tokenSource.Cancel();

            _workingThread.Join(2000);
            _workingThread.Abort();
            _workingThread = null;

            _metaDatesThread.Join(2000);
            _metaDatesThread.Abort();
            _metaDatesThread = null;
        }

        private void WorkingThreadTask()
        {
            int i = 0;
            while (!_token.IsCancellationRequested)
            {
                Process[] tasks = Process.GetProcesses();
                var previous = new HashSet<int>(_tasksList.Keys);

                foreach (var task in tasks)
                {
                    _tasksList.GetOrAdd(task.Id, new Task(task));
                    previous.Remove(task.Id);

                    if (_token.IsCancellationRequested) return;
                }

                foreach (var task in previous)
                {
                    Task t;
                    _tasksList.TryRemove(task, out t);

                    if (_token.IsCancellationRequested) return;
                }

                OnPropertyChanged("ViewSource");

                for (int j = 0; j < 10; j++)
                {
                    Thread.Sleep(500);
                    if (_token.IsCancellationRequested) break;
                }
                i++;
            }
        }

        private void UpdateMetaDates()
        {
            int i = 0;
            while (!_token.IsCancellationRequested)
            {
                foreach (var task in _tasksList.Values)
                {
                    task.UpdateTask();
                    if (_token.IsCancellationRequested) return;
                }

                for (int j = 0; j < 4; j++)
                {
                    Thread.Sleep(500);
                    if (_token.IsCancellationRequested) break;
                }

                i++;
            }
        }

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
