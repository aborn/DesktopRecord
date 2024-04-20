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
        public static bool Start(string waterText)
        {
            if (!File.Exists(ffmpegPath))
                return false;
            Size size = WindowHelper.GetMonitorSize();
            string dpi = size.Width + "x" + size.Height;
            string fileName = "in.mp4"; //String.Format("{0}{1}{2}", "星光录屏_", DateTime.Now.ToString("yyyyMMddHHmmss"), ".mp4");
            string fullFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            FileInfo fileInfo = new FileInfo(fullFileName);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }

            // string waterText = "星光录屏";
            string waterMarker = String.IsNullOrWhiteSpace(waterText) ? string.Empty :
                String.Format(" -vf \"drawtext=fontsize=60:fontfile=HarmonyOS_Sans_SC_Bold.ttf:text='{0}':x=20:y=20:fontcolor=#37aefe\"", waterText);

            var processInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                // ffmpeg -f gdigrab -i desktop -f dshow -i audio="virtual-audio-capturer" -vcodec libx264 -acodec libmp3lame -s 1280x720 -r 15 e:/temp/temp.mkv
                // 
                // Arguments = "-f gdigrab -framerate 30 -offset_x 0 -offset_y 0 -video_size 1920x1080 -i desktop -c:v libx264 -preset ultrafast -crf 0 " + DateTime.Now.ToString("yyyyMMddHHmmss") + "_DesktopRecord.mp4",

                /*** 模式一：只录制视屏，无声音 */
                // Arguments = String.Format("-f gdigrab -framerate 30 -offset_x 0 -offset_y 0 -video_size {0} -i desktop -c:v libx264 -preset ultrafast -crf 0 {1}",
                //    dpi, fileName),  


                /***
                 * 模式二：仅录制扬声器的音（无麦克风的声音），电脑里需要安装 screen capture recorder  https://github.com/rdp/screen-capture-recorder-to-video-windows-free
                 * audio=\"virtual-audio-capturer\"  audio="麦克风 (Realtek(R) Audio)"
                 * .\ffmpeg.exe  -list_devices true -f dshow -i dummy */
                //Arguments = String.Format("-f gdigrab -i desktop -f dshow -i audio=\"virtual-audio-capturer\" -vcodec libx264 -preset ultrafast -acodec libmp3lame -s {0} -r 15 {1}",
                //    dpi, fileName),

                /*** 麦克风 + 扬声器，但有Bug，音频和视频不同步 */
                // Arguments = String.Format("-f gdigrab -i desktop -f dshow -i audio=\"麦克风 (Realtek(R) Audio)\"  -f dshow -i audio=\"virtual-audio-capturer\" -filter_complex amix=inputs=2:duration=first:dropout_transition=2 -vcodec libx264 -preset ultrafast -acodec libmp3lame -s {0} -r 15 {1}",
                //    dpi, fileName),  


                /*** 麦克风 + 扬声器，但有Bug，保持音频和视频的同步, 增加字幕 */
                Arguments = String.Format(" -f dshow -i audio=\"麦克风 (Realtek(R) Audio)\" -thread_queue_size 512 -f dshow -i audio=\"virtual-audio-capturer\" -filter_complex amix=inputs=2:duration=first:dropout_transition=2 -thread_queue_size 64 -f gdigrab -i desktop -vcodec libx264 -preset ultrafast -codec:a aac -ac 2 -ar 44100 -tune zerolatency -q:a 0 -s {0} {2} {1}",
                       dpi, fileName, waterMarker),  // 麦克风 + 扬声器

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
            string resultFileName = String.Format("{0}{1}{2}", "星光录屏_", DateTime.Now.ToString("yyyyMMddHHmmss"), ".mp4");
            string resultFileNameOrigin = String.Format("{0}{1}{2}", "星光录屏_", DateTime.Now.ToString("yyyyMMddHHmmss"), "_原片.mp4");

            //if (String.IsNullOrEmpty(waterMarker))
            //{
            //    string inFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "in.mp4");
            //    File.Move(inFileName, resultFileName);
            //    return true;
            //}

            // 视频压缩
            string withoutWaterMarkerArgs = String.Format("-i in.mp4 -vcodec libx264 -codec:a aac -ac 2 -ar 44100 -tune zerolatency  {0}", resultFileName);
            //string warterMarkerArgs = String.Format(
            //    "-i in.mp4 -vf \"drawtext=fontsize=60:fontfile=HarmonyOS_Sans_SC_Bold.ttf:text='{0}':x=20:y=20:fontcolor=#37aefe\" {1}",
            //    waterMarker,
            //    resultFileName);
            var processInfo =
                new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = withoutWaterMarkerArgs,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
            waterMarkerProcess = new Process { StartInfo = processInfo };
            waterMarkerProcess.Start();
            waterMarkerProcess.WaitForExit();
            string inFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "in.mp4");
            File.Move(inFileName, resultFileNameOrigin);
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
