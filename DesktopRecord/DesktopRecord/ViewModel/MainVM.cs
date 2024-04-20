using DesktopRecord.Helper;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using WPFDevelopers.Controls;
using WPFDevelopers.Helpers;
using Win32 = DesktopRecord.Helper.Win32;
using System.IO;
using System.Runtime.InteropServices;

namespace DesktopRecord.ViewModel
{

    public class MainVM : ViewModelBase
    {
        private DispatcherTimer tm = new DispatcherTimer();

        public int currentCount = 0;

        private RecordEnums _recordEnums;
        public RecordEnums RecordEnums
        {
            get { return _recordEnums; }
            set
            {
                _recordEnums = value;
                NotifyPropertyChange(nameof(RecordEnums));
            }
        }

        private string myTime = "开始录制";

        public string MyTime
        {
            get { return myTime; }
            set
            {
                myTime = value;
                NotifyPropertyChange("MyTime");
            }
        }


        private bool isStart = true;

        public bool IsStart
        {
            get { return isStart; }
            set
            {
                isStart = value;
                NotifyPropertyChange("IsStart");
            }
        }


        private bool _isShow;

        public bool IsShow
        {
            get { return _isShow; }
            set
            {
                _isShow = value;
                NotifyPropertyChange("IsShow");
            }
        }

        private string _waterMaker = "";

        public string WaterMaker
        {
            get
            {
                return _waterMaker;
            }
            set
            {
                if (value == _waterMaker) { return; }
                _waterMaker = value;
                NotifyPropertyChange(nameof(WaterMaker));
            }
        }


        private ICommand openFiles;

        

        public ICommand OpenFiles
        {
            get
            {
                return openFiles ?? (openFiles = new RelayCommand(p =>
                {
                    string fileDir = AppDomain.CurrentDomain.BaseDirectory;
                    string fileName = FileHelper.GetLatestFileTimeInfo(fileDir, ".mp4").FileName;
                    string filePath = Path.Combine(fileDir, fileName);
                    FileHelper.OpenFilePathLocation(filePath);
                    // Process.Start(AppDomain.CurrentDomain.BaseDirectory);
                }, a =>
                {
                    return IsStart;
                }));
            }
        }

        private ICommand myStart;

        public ICommand MyStart
        {
            get
            {
                return myStart ?? (myStart = new RelayCommand(p =>
                {
                    App.Current.MainWindow.WindowState = System.Windows.WindowState.Minimized;
                    if (!FFmpegHelper.Start(_waterMaker))
                    {
                        App.Current.MainWindow.WindowState = System.Windows.WindowState.Normal;
                        Message.Push("未找到 【ffmpeg.exe】,请下载", System.Windows.MessageBoxImage.Error);
                        return;
                    }
                    tm.Tick += tm_Tick;
                    tm.Interval = TimeSpan.FromSeconds(1);
                    tm.Start();
                    IsStart = false;
                }, a =>
                 {
                     return IsStart;
                 }));
            }
        }
        private void tm_Tick(object sender, EventArgs e)
        {
            currentCount++;
            MyTime = "录制中(" + currentCount + "s)";
        }

        private void tm_Tick_WaterMaker(object sender, EventArgs e)
        {
            currentCount++;
            string dots = currentCount % 3 == 2 ? "......" : (currentCount % 3 == 1 ? "...." : "..");
            MyTime = $"处理中{dots}(" + currentCount + "s)";
        }
        /// <summary>
        /// 获取或设置
        /// </summary>
        private ICommand myStop;

        /// <summary>
        /// ffmpeg模式下的停止按键的handler
        /// </summary>
        public ICommand MyStop
        {
            get
            {
                return myStop ?? (myStop = new RelayCommand(p =>
                           {
                               var recordTask = new Task(() =>
                               {
                                   FFmpegHelper.Stop();
                                   MyTime = "压缩处理";
                                   tm.Stop();
                                   currentCount = 0;
                                   IsShow = false; // 停止Button是否显示
                                   tm.Tick += tm_Tick_WaterMaker;
                                   tm.Interval = TimeSpan.FromSeconds(1);
                                   tm.Start();
                               });
                               recordTask.Start();
                               var waterMarkerTask = recordTask.ContinueWith(previousTask =>
                               {
                                   MyTime = "处理中";
                                   IsShow = false;
                                   currentCount = 0;
                                   FFmpegHelper.AddWarterMarker(_waterMaker);
                               }, TaskContinuationOptions.OnlyOnRanToCompletion);
                               waterMarkerTask.ContinueWith(previousTask =>
                               {
                                   MyTime = "开始录制";
                                   IsShow = false;
                                   IsStart = true; // 录屏Button是否显示
                                   tm.Stop();
                                   currentCount = 0;
                                   // Process.Start(AppDomain.CurrentDomain.BaseDirectory);
                                   string fileDir = AppDomain.CurrentDomain.BaseDirectory;
                                   string fileName = FileHelper.GetLatestFileTimeInfo(fileDir, ".mp4").FileName;
                                   string filePath = Path.Combine(fileDir, fileName);
                                   FileHelper.OpenFilePathLocation(filePath);
                               }, TaskContinuationOptions.OnlyOnRanToCompletion);

                           }, a =>
            {
                return !IsStart;
            }));
            }
        }
        public ICommand RecordCommand { get; }
        public ICommand RecordStopCommand { get; }

        public MainVM()
        {
            RecordCommand = new RelayCommand(Record, CanExecuteRecordCommand);
            RecordStopCommand = new RelayCommand(RecordStop);
            WaterMaker = "星光录屏"; // 初始值，但没有显示在UI上，为什么？
        }
        void Record(object parameter)
        {
            App.Current.MainWindow.WindowState = System.Windows.WindowState.Minimized;
            switch (RecordEnums)
            {
                case RecordEnums.FFmpeg:
                    break;
                case RecordEnums.WindowsAPI:
                    Win32.Start();
                    break;
                case RecordEnums.Accord:
                    AccordHelper.Start();
                    break;
                default:
                    break;
            }
            IsStart = false;
        }

        private bool CanExecuteRecordCommand(object parameter)
        {
            return IsStart;
        }
        void RecordStop(object parameter)
        {
            var task = new Task(() =>
            {
                switch (RecordEnums)
                {
                    case RecordEnums.FFmpeg:
                        break;
                    case RecordEnums.WindowsAPI:
                        Win32.Stop();
                        Win32.Save($"DesktopRecord_{DateTime.Now.ToString("yyyyMMddHHmmss")}.gif");
                        break;
                    case RecordEnums.Accord:
                        AccordHelper.Stop();
                        break;
                    default:
                        break;
                }
                IsShow = true;
            });
            task.ContinueWith(previousTask =>
            {
                IsShow = false;
                IsStart = true;
                Process.Start(AppDomain.CurrentDomain.BaseDirectory);
            }, TaskScheduler.FromCurrentSynchronizationContext());
            task.Start();
        }

    }
}
