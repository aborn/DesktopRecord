﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace DesktopRecord.Helper
{
    public class FFmpegHelper
    {
        #region 模拟控制台信号需要使用的api

        [DllImport("kernel32.dll")]
        static extern bool GenerateConsoleCtrlEvent(int dwCtrlEvent, int dwProcessGroupId);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleCtrlHandler(IntPtr handlerRoutine, bool add);

        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();

        #endregion
        // ffmpeg进程
        static Process _process;

        static Process waterMarkerProcess;

        // ffmpeg.exe实体文件路径
        static string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");

        /// <summary>
        /// 功能: 开始录制
        /// </summary>
        public static bool Start()
        {
            if (!File.Exists(ffmpegPath))
                return false;
            Size size = WindowHelper.GetMonitorSize();
            string dpi = size.Width + "x" + size.Height;
            string fileName = "in.mp4"; //String.Format("{0}{1}{2}", "中慧星光_", DateTime.Now.ToString("yyyyMMddHHmmss"), ".mp4");
            string fullFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            FileInfo fileInfo = new FileInfo(fullFileName);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
            var processInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                // Arguments = "-f gdigrab -framerate 30 -offset_x 0 -offset_y 0 -video_size 1920x1080 -i desktop -c:v libx264 -preset ultrafast -crf 0 " + DateTime.Now.ToString("yyyyMMddHHmmss") + "_DesktopRecord.mp4",
                Arguments = String.Format("-f gdigrab -framerate 30 -offset_x 0 -offset_y 0 -video_size {0} -i desktop -c:v libx264 -preset ultrafast -crf 0 {1}",
                    dpi, fileName),
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            _process = new Process { StartInfo = processInfo };
            _process.Start();
            return true;
        }

        /// <summary>
        /// 功能：给视频添加文字水印
        /// </summary>
        /// <param name="waterMarker"></param>
        /// <returns></returns>
        public static bool AddWarterMarker(string waterMarker)
        {
            if (String.IsNullOrEmpty(waterMarker))
            {
                return true;
            }

            string fileName = String.Format("{0}{1}{2}", "星光录屏_", DateTime.Now.ToString("yyyyMMddHHmmss"), ".mp4");
            var processInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
               
                Arguments = String.Format("-i in.mp4 -vf \"drawtext=fontsize=60:fontfile=HarmonyOS_Sans_SC_Bold.ttf:text='{0}':x=20:y=20:fontcolor=#37aefe\" {1}",
                    waterMarker,
                    fileName),
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            waterMarkerProcess = new Process { StartInfo = processInfo };
            waterMarkerProcess.Start();
            waterMarkerProcess.WaitForExit();
            return true;
        }

        /// <summary>
        /// 功能: 停止录制
        /// </summary>
        public static void Stop()
        {
            if (_process == null) return;
            AttachConsole(_process.Id);
            SetConsoleCtrlHandler(IntPtr.Zero, true);
            GenerateConsoleCtrlEvent(0, 0);
            FreeConsole();
            _process.StandardInput.Write("q");
            if (!_process.WaitForExit(10000))
            {
                _process.Kill();
            }
        }
    }
}
