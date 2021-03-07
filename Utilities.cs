﻿using System;
using System.Text;
using System.Diagnostics;
using JR.Utils.GUI.Forms;
using System.Net;
using System.Windows.Forms;
using System.Net.Http;
using System.IO;
using AndroidSideloader;

namespace Spoofer
{
    class Utilities
    {
        public static string RandomPackageName()
        {
            return $"com.{Utilities.randomString(rand.Next(3, 8))}.{Utilities.randomString(rand.Next(3, 8))}";
        }

        public static string CommandOutput = "";
        public static string CommandError = "";

        public static void ExecuteCommand(string command)
        {
            var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;

            var process = Process.Start(processInfo);

            process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                CommandOutput += e.Data;
            process.BeginOutputReadLine();

            process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                CommandError += e.Data;
            process.BeginErrorReadLine();

            process.WaitForExit();

            Console.WriteLine("ExitCode: {0}", process.ExitCode);
            process.Close();
        }

        public static void Melt()
        {
            Process.Start(new ProcessStartInfo()
            {
                Arguments = "/C choice /C Y /N /D Y /T 5 & Del \"" + Application.ExecutablePath + "\"",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = "cmd.exe"
            });
        }
        static Random rand = new Random();
        public static string randomString(int length)
        {
            string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            StringBuilder res = new StringBuilder();

            int randomInteger = rand.Next(0, valid.Length);
            while (0 < length--)
            {
                res.Append(valid[randomInteger]);
                randomInteger = rand.Next(0, valid.Length);
            }
            return res.ToString();
        }
        public static string processError = string.Empty;
        public static string startProcess(string process, string path, string command)
        {
            Logger.Log($"Ran process {process} with command {command} in path {path}");
            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.RedirectStandardError = true;
            cmd.StartInfo.WorkingDirectory = path;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();
            cmd.StandardInput.WriteLine(command);
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit();
            string error = cmd.StandardError.ReadToEnd();
            if (error.Length > 1)
                processError = error;
            var output = cmd.StandardOutput.ReadToEnd();
            Logger.Log($"Output: {output}");
            Logger.Log($"Error: {error}");
            return output;
        }

    }

    class Zip
    {
        public static void ExtractFile(string sourceArchive, string destination)
        {
            if (!File.Exists(Environment.CurrentDirectory + "\\7z.exe"))
            {
                WebClient client = new WebClient();
                client.DownloadFile("https://github.com/nerdunit/androidsideloader/raw/master/7z.exe", "7z.exe");
                client.DownloadFile("https://github.com/nerdunit/androidsideloader/raw/master/7z.dll", "7z.dll");
            }
            ProcessStartInfo pro = new ProcessStartInfo();
            pro.WindowStyle = ProcessWindowStyle.Hidden;
            pro.FileName = "7z.exe";
            pro.Arguments = string.Format("x \"{0}\" -y -o\"{1}\"", sourceArchive, destination);
            Process x = Process.Start(pro);
            x.WaitForExit();
        }
    }

    class Updater
    {

        public static string AppName { get; set; }
        public static string RawGitHubUrl { get; set; } //https://raw.githubusercontent.com/nerdunit/androidsideloader
        public static string GitHubUrl { get; set; }
        static readonly public string LocalVersion = "1.17SU1";
        public static string currentVersion = string.Empty;
        public static string changelog = string.Empty;

        private static bool IsUpdateAvailable()
        {
            HttpClient client = new HttpClient();
            try
            {
                currentVersion = client.GetStringAsync($"{RawGitHubUrl}/master/version").Result;
                currentVersion = currentVersion.Remove(currentVersion.Length - 1);
                changelog = client.GetStringAsync($"{RawGitHubUrl}/master/changelog.txt").Result;
            }
            catch { return false; }
            return LocalVersion != currentVersion;
        }
        public static void Update()
        {
            if (IsUpdateAvailable())
                doUpdate();
        }
        private static void doUpdate()
        {
            DialogResult dialogResult = FlexibleMessageBox.Show($"There is a new update you have version {LocalVersion}, do you want to update?\nCHANGELOG\n{changelog}", $"Version {currentVersion} is available", MessageBoxButtons.YesNo);
            if (dialogResult != DialogResult.Yes)
                return;

            try
            {
                using (var fileClient = new WebClient())
                {
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    Logger.Log($"Downloading update from {RawGitHubUrl}/releases/download/v{currentVersion}/{AppName}.exe to {AppName} v{currentVersion}.exe");
                    fileClient.DownloadFile($"{GitHubUrl}/releases/download/v{currentVersion}/{AppName}.exe", $"{AppName} v{currentVersion}.exe");
                }

                Utilities.Melt();
                Logger.Log("Starting {AppName} v{currentVersion}.exe");
                Process.Start($"{AppName} v{currentVersion}.exe");
            }
            catch { }

            Environment.Exit(0);
        }
    }
}
