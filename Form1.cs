using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

//changeRDPPort.exe
//Remote Desktop Environment Maintenance Tools
//This software changes the RDP port number in the registry to the argument value and restarts. Please use it with caution.
//The purpose and implementation procedures of this software will be explained in readme.md.

//リモートデスクトップ環境保守ツール
//本ソフトは、レジストリのrdpポート番号を引数値に変更して再起動します。使用には注意してください。
//本ソフトの使用目的と実施手順等は、readme.mdで解説します。

//(c)2025 teamwind japan n.hayashi

namespace changeRDPPortCS
{
    public partial class Form1 : Form
    {
        #region "Runtimes"

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();

        [System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle,
            uint DesiredAccess,
            out IntPtr TokenHandle);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true,
            CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool LookupPrivilegeValue(string lpSystemName,
            string lpName,
            out long lpLuid);

        [System.Runtime.InteropServices.StructLayout(
           System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
        private struct TOKEN_PRIVILEGES
        {
            public int PrivilegeCount;
            public long Luid;
            public int Attributes;
        }

        [System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle,
            bool DisableAllPrivileges,
            ref TOKEN_PRIVILEGES NewState,
            int BufferLength,
            IntPtr PreviousState,
            IntPtr ReturnLength);

        public static void AdjustToken()
        {
            const uint TOKEN_ADJUST_PRIVILEGES = 0x20;
            const uint TOKEN_QUERY = 0x8;
            const int SE_PRIVILEGE_ENABLED = 0x2;
            const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";

            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return;
            IntPtr procHandle = GetCurrentProcess();
            IntPtr tokenHandle;
            OpenProcessToken(procHandle,
                TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out tokenHandle);
            TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES();
            tp.Attributes = SE_PRIVILEGE_ENABLED;
            tp.PrivilegeCount = 1;
            LookupPrivilegeValue(null, SE_SHUTDOWN_NAME, out tp.Luid);
            AdjustTokenPrivileges(tokenHandle, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
            CloseHandle(tokenHandle);
        }

        public enum ExitWindows : uint
        {
            EWX_LOGOFF = 0x00,
            EWX_SHUTDOWN = 0x01,
            EWX_REBOOT = 0x02,
            EWX_POWEROFF = 0x08,
            EWX_RESTARTAPPS = 0x40,
            EWX_FORCE = 0x04,
            EWX_FORCEIFHUNG = 0x10,
        }
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        public static extern bool ExitWindowsEx(ExitWindows uFlags, int dwReason);

        #endregion

        #region "Another shutdown process 別のシャットダウン処理"

        //The following codes are different ways to restart. Please use them as appropriate.
        //Please note that shutdown.exe works differently depending on the Windows version.

        //以下のコードは別の再起動方法です。適宜切り分けてください。
        //shutdown.exeは、Windowsバージョンによって動作に違いがあります。注意してください。

        private void otherReboot()
        {
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
            psi.FileName = "shutdown.exe";
            psi.Arguments = "/r/f";
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            System.Diagnostics.Process p = System.Diagnostics.Process.Start(psi);
        }

        #endregion

        #region "onload"

        Int32 port;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Argument. 引数取得
            //If there are no arguments end. 引数無しは終了
            //If you want to set it directly, delete the code below. 直接セットする場合は、以下のコードを削除してください。
            string[] cmds = System.Environment.GetCommandLineArgs();
            if(cmds.Length == 2)
            {
                try {
                    port = Convert.ToInt32(cmds[1]);
                } catch {
                    this.Close();
                }
            } else {
                this.Close();
            }

            //Registry rdp port change. レジストリrdpポート変更
            //If you do not use the argument, set it directly. 引数を使用しない場合は、直接セットしてください。
            Microsoft.Win32.Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp", "PortNumber", port);

            //Privilege Activation 特権有効化
            AdjustToken();
            //reboot
            ExitWindowsEx(ExitWindows.EWX_REBOOT | ExitWindows.EWX_FORCE, 0);

            this.Close();
        }

        #endregion

    }
}
