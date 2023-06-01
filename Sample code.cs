using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Accord.Video;
using Accord.Video.FFMPEG;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System;

namespace Timelapser_version_1._2
{
    class ScreenRecorder
    {
        public static int userwidth = 1;
        public static int userhight = 1;
        public static int v=0;
        public static string handle_name;
        int width;
        int height;
        //Video variables:
        public static Rectangle bounds;
        private string outputPath = "";
        private string tempPath = "";
        private int fileCount = 1;
        private List<string> inputImageSequence = new List<string>();
        //File variables:
        private string audioName = "mic.wav";
        private string videoName = "video.mp4";
        private string finalName = "FinalVideo.mp4-";

        //Time variable:
        Stopwatch watch = new Stopwatch();
        //Audio variables:
        public static class NativeMethods
        {   
            [DllImport("winmm.dll", EntryPoint = "mciSendStringA", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
            public static extern int record(string lpstrCommand, string lpstrReturnString, int uReturnLength, int hwndCallback);
        }

        //ScreenRecorder Object:
        public ScreenRecorder(Rectangle b, string outPath)
        {
            //Create temporary folder for screenshots:
            CreateTempFolder("tempScreenCaps");

            //Set variables:
            bounds = b;
            outputPath = outPath;
        }

        //Create temporary folder:
        private void CreateTempFolder(string name)
        {
            //Check if a C or D drive exists:
            if (Directory.Exists("D://"))
            {
                string pathName = $"D://{name}";
                Directory.CreateDirectory(pathName);
                tempPath = pathName;
            }
            else
            {
                string pathName = $"C://Documents//{name}";
                Directory.CreateDirectory(pathName);
                tempPath = pathName;
            }
        }

        //Change final video name:
        public void setVideoName(string name)
        {
                finalName = name;
        }

        //Delete all files and directory:
        private void DeletePath(string targetDir)
        {
            string[] files = Directory.GetFiles(targetDir);
            string[] dirs = Directory.GetDirectories(targetDir);

            //Delete each file:
            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            //Delete the path:
            foreach (string dir in dirs)
            {
                DeletePath(dir);
            }

            Directory.Delete(targetDir, false);
        }

        //Delete all files except the one specified:
        private void DeleteFilesExcept(string targetDir, string excDir)
        {
            string[] files = Directory.GetFiles(targetDir);

            //Delete each file except specified:
            foreach (string file in files)
            {
                if (file != excDir)
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
            }
        }

        //Clean up program on crash:
        public void cleanUp()
        {
            if (Directory.Exists(tempPath))
            {
                DeletePath(tempPath);
            }
        }

        //Return elapsed time:
        public string getElapsed()
        {
            return string.Format("{0:D2}:{1:D2}:{2:D2}", watch.Elapsed.Hours, watch.Elapsed.Minutes, watch.Elapsed.Seconds);
        }

        //Record video:
        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll")]
        private static extern int SetForegroundWindow(IntPtr hWnd);

        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        private static extern IntPtr ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);
        public Bitmap CaptureApplication(string procName)
        {
            Process proc;

            // Cater for cases when the process can't be located.
            try
            {
                proc = Process.GetProcessesByName(procName)[0];
            }
            catch (IndexOutOfRangeException e)
            {
                return null;
            }
            //focus on the application
            SetForegroundWindow(proc.MainWindowHandle);
            ShowWindow(proc.MainWindowHandle, SW_RESTORE);
            Thread.Sleep(1000);
            Rect rect = new Rect();
            IntPtr error = GetWindowRect(proc.MainWindowHandle, ref rect);
            // sometimes it gives error.
            while (error == (IntPtr)0)
            {
                error = GetWindowRect(proc.MainWindowHandle, ref rect);
            }

            width = rect.right - rect.left;
            height = rect.bottom - rect.top;

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Graphics.FromImage(bmp).CopyFromScreen(rect.left,rect.top,0,0,new Size(width, height),CopyPixelOperation.SourceCopy);
            return bmp;
        }
        public void RecordVideo()
        {
            //Keep track of time:
             watch.Start();
            //Save screenshot:
            
            using (var image = CaptureApplication(handle_name)) 
            {
                string name = tempPath + "//screenshot-" + fileCount + ".png";
                image.Save(name, ImageFormat.Png);
                inputImageSequence.Add(name);
                fileCount++;

            }
            
        }
        //Compare images
        public static List<bool> GetHash(Bitmap bmpSource)
        {

            List<bool> lResult = new List<bool>();
            //create new image with 16x16 pixel
            Bitmap bmpMin = new Bitmap(bmpSource, new System.Drawing.Size(16,16));
            for (int j = 0; j < bmpMin.Height; j++)
            {
                for (int i = 0; i < bmpMin.Width; i++)
                {
                    //reduce colors to true / false                
                    lResult.Add(bmpMin.GetPixel(i, j).GetBrightness() < 0.5f);
                }
            }
            return lResult;

            
        }
        //Record audio:
        public void RecordAudio()
        {
            NativeMethods.record("open new Type waveaudio Alias recsound", "", 0, 0);
            NativeMethods.record("record recsound", "", 0, 0);
        }
       
        //Save audio file:
       private void SaveAudio()
        {
            string audioPath = "save recsound " + outputPath + "//" + audioName;
            NativeMethods.record(audioPath, "", 0, 0);
            NativeMethods.record("close recsound", "", 0, 0);
        }

        //Save video file:

        public void SaveVideo(int width, int height, int frameRate)
        {
            for (int k = 1; k < fileCount; k++)
            {
                if (Directory.Exists(tempPath + "//screenshot-" + k + ".png") && Directory.Exists(tempPath + "//screenshot-" + (k + 1) + ".png"))
                {
                    List<bool> iHash1 = GetHash(new Bitmap(tempPath + "//screenshot-" + k + ".png"));
                    List<bool> iHash2 = GetHash(new Bitmap(tempPath + "//screenshot-" + (k + 1) + ".png"));
                    //determine the number of equal pixel (x of 256)
                    long equalElements = iHash1.Zip(iHash2, (i, j) => i == j).Count(eq => eq);
                    Console.WriteLine("Process: {0}", equalElements);
                    if (equalElements < 250)
                    {
                        var filePath = tempPath + "//screenshot-" + k + ".png";
                        File.Delete(filePath);
                    }
                }
            }

            using (VideoFileWriter vFWriter = new VideoFileWriter())
            {
                vFWriter.Open(outputPath + "//" + videoName, width, height, frameRate, VideoCodec.MPEG4);
                //Make each screenshot into a video frame:
                foreach (string imageLocation in inputImageSequence)
                {
                    Bitmap imageFrame = System.Drawing.Image.FromFile(imageLocation) as Bitmap;
                    vFWriter.WriteVideoFrame(imageFrame);
                    imageFrame.Dispose();
                }

                //Close:
                vFWriter.Close();
            }
        }
        
        //Combine video and audio files:
        private void CombineVideoAndAudio(string video, string audio)
        {
            //FFMPEG command to combine video and audio:
            string args = $"/c ffmpeg -i \"{video}\" -i \"{audio}\" -shortest {finalName}";
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                CreateNoWindow = false,
                FileName = "cmd.exe",
                WorkingDirectory = outputPath,
                Arguments = args
            };

            //Execute command:
            using (Process exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();
            }
        }
       
        public void Stop()
        {
            //Stop watch:
            watch.Stop();

            //Video variables:
            int frameRate =v+10;
            
            //Save audio:
            SaveAudio();

            //Save video:
            SaveVideo(width, height, frameRate);

            //Combine audio and video files:
            CombineVideoAndAudio(videoName, audioName);

            //Delete the screenshots and temporary folder:
            DeletePath(tempPath);

            //Delete separated video and audio files:
            DeleteFilesExcept(outputPath, outputPath + "\\" + finalName);
        }
    }
}
