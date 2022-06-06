using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Threading;
using System.Windows.Forms;

namespace WindowsStartup
{
    internal class Program
    {
        private static void ClearFolder(string FolderName)
        {
            DirectoryInfo dir = new DirectoryInfo(FolderName);

            foreach (FileInfo fi in dir.GetFiles())
            {
                try
                {
                    fi.Delete();
                }
                catch (Exception) { } // Ignore all exceptions
            }

            foreach (DirectoryInfo di in dir.GetDirectories())
            {
                ClearFolder(di.FullName);
                try
                {
                    di.Delete();
                }
                catch (Exception) { } // Ignore all exceptions
            }
        }

        private static void CacheDirClean(string FolderPath)
        {
            try
            {
                foreach (string dir in Directory.EnumerateDirectories(FolderPath))
                {
                    if (dir.Contains("Cache"))
                    {
                        Directory.Delete(dir, true);
                        Wait(1000);
                        _ = Directory.CreateDirectory(dir);
                    }
                    if (dir.Contains("cache"))
                    {
                        Directory.Delete(dir, true);
                        Wait(1000);
                        _ = Directory.CreateDirectory(dir);
                    }
                }
            }
            catch (Exception) { }
        }

        public static void Wait(int milliseconds)
        {
            System.Windows.Forms.Timer timer1 = new System.Windows.Forms.Timer();
            if (milliseconds == 0 || milliseconds < 0)
            {
                return;
            }

            // Console.WriteLine("start wait timer");
            timer1.Interval = milliseconds;
            timer1.Enabled = true;
            timer1.Start();

            timer1.Tick += (s, e) =>
            {
                timer1.Enabled = false;
                timer1.Stop();
            };

            while (timer1.Enabled)
            {
                Application.DoEvents();
            }
        }

        private static void Main()
        {
            // Create definition
            TaskDefinition td = TaskService.Instance.NewTask();

            // Hide settings
            td.Settings.Hidden = true;

            // Set the run level to the highest privilege
            td.Principal.RunLevel = TaskRunLevel.Highest;

            // Description
            td.RegistrationInfo.Description = "Cleans temporary files on boot. And restarts the explorer and dwm (Desktop Window Manager)";

            // These settings will ensure it runs even if on battery power
            td.Settings.DisallowStartIfOnBatteries = false;
            td.Settings.StopIfGoingOnBatteries = false;
            td.Settings.Compatibility = TaskCompatibility.V2_3;

            LogonTrigger lt = new LogonTrigger
            {
                Delay = TimeSpan.FromSeconds(15)
            };
            _ = td.Triggers.Add(lt);

            _ = td.Actions.Add(Application.ExecutablePath);

            // Register the task in the root folder of the local machine
            _ = TaskService.Instance.RootFolder.RegisterTaskDefinition("Windows Startup Cleaning", td);

            _ = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            FilePaths FileLocation = new FilePaths();
            _ = FileLocation + "\\Microsoft\\Windows\\Start Menu\\Programs\\Startup\\";

            RegistryKey StartupKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            StartupKey.SetValue("Windows_Startup_Cleaner", Application.ExecutablePath);

            PowerShell script = PowerShell.Create();
            //_ = script.AddScript("Set-ItemProperty \"HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" -Name \"ConsentPromptBehaviorAdmin\" -Value \"0\" ");
            _ = script.AddScript("Stop-Process -Name explorer -Force -PassThru");
            _ = script.Invoke();
            _ = Process.Start("cmd.exe", "/c taskkill /f /im explorer.exe");
            Thread.Sleep(500);
            _ = Process.Start(Environment.SystemDirectory + "\\..\\explorer.exe");
            _ = Process.Start("cmd.exe", "/c taskkill /f /t /im dwm.exe");

            foreach (string dir in FileLocation.GraphicDrivers)
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch (Exception) { } // Ignore all exceptions
            }

            foreach (string dir in FileLocation.Temporary)
            {
                try
                {
                    ClearFolder(dir);
                }
                catch (Exception) { } // Ignore all exceptions
            }

            foreach (string dir in FileLocation.TemporaryCachePaths)
            {
                try
                {
                    CacheDirClean(dir);
                }
                catch (Exception) { } // Ignore all exceptions
            }

            // Deleting "*.db"
            string NetCache = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\AppData\\Local\\Microsoft\\Windows\\Explorer\\";
            DirectoryInfo d = new DirectoryInfo(NetCache);

            FileInfo[] Files = d.GetFiles("*.db"); //Getting db files

            foreach (FileInfo file in Files)
            {
                try
                {
                    string str = file.FullName;
                    File.Delete(str);
                }
                catch (Exception) { }
            }
            Thread.Sleep(500);
            Application.Exit();
        }
    }
}