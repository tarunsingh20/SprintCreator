using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace STaRZ.WinAPI
{
    public class WinAPI
    {
        #region Import Window API DLL's

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport("user32.dll")]
        internal static extern bool EnumChildWindows(IntPtr hwnd, EnumWindowProc func, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, [MarshalAs(UnmanagedType.LPStr)] string lParam);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        #endregion

        #region Variables

        internal delegate bool EnumWindowProc(IntPtr hWnd, IntPtr parameter);
        private const int WM_SETTEXT = 0x0C;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;

        private const long waitPeriod = 30000;

        private const string securityDialogClassId = "#32770";
        private const string securityDialogCaption = "Windows Security";
        private const string ieProcessName = "iexplore";

        #endregion

        #region Internal Methods

        internal static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointer);
            List<IntPtr> list = gch.Target as List<IntPtr>;
            if (list == null)
            {
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            }
            list.Add(handle);
            //  You can modify this to check to see if you want to cancel the operation, then return a null here
            return true;
        }

        #endregion

        #region Public Methods

        public static List<IntPtr> GetChildWindows(IntPtr parent)
        {
            List<IntPtr> result = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(result);
            try
            {
                EnumWindowProc childProc = new EnumWindowProc(EnumWindow);
                EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }
            return result;
        }

        public static string GetText(IntPtr hWnd)
        {
            // Allocate correct string length first
            int length = GetWindowTextLength(hWnd);
            StringBuilder sb = new StringBuilder(length + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        /// <summary>
        /// Handle the standard "Windows Security" dialog by entering the provided user name and password and pressing the OK button.
        /// </summary>
        /// <param name="_domainName">
        /// Domain name to be filled.
        /// </param>
        /// <param name="_userName">
        /// Username to be filled in. Enter in the format domain\username.
        /// </param>        
        /// <param name="_password">
        /// Password to be filled.
        /// </param>
        public static void HandleWindowsSecurityDialog(string _domainName, string _userName, System.Security.SecureString _password)
        {
            IntPtr usernameTextBox = IntPtr.Zero;
            IntPtr passwordTextBox = IntPtr.Zero;
            IntPtr okButton = IntPtr.Zero;

            StringBuilder stringBuilder = null;
            IntPtr securityDialogHandle = IntPtr.Zero;
            List<IntPtr> childWindowHandles = null;
            Process[] ieProcess = Process.GetProcessesByName(ieProcessName);

            Stopwatch stopWatch = new Stopwatch();

            stopWatch.Start();

            // Let us watch for 30 seconds for IE to open and popup the the Windows Security dialog
            while (stopWatch.ElapsedMilliseconds <= waitPeriod)
            {
                securityDialogHandle = FindWindowEx(IntPtr.Zero, IntPtr.Zero, securityDialogClassId, securityDialogCaption);

                // Windows Security dialog found
                if (securityDialogHandle != IntPtr.Zero)
                {
                    break;
                }
            }

            stopWatch.Stop();
            stopWatch = null;
            Thread.Sleep(100);

            childWindowHandles = GetChildWindows(securityDialogHandle);

            stringBuilder = new StringBuilder(100);

            // Loop through all the window handles and get the username & password textbox as well as the OK button
            foreach (IntPtr handlePointer in childWindowHandles)
            {
                if (GetClassName(handlePointer, stringBuilder, stringBuilder.Capacity) != 0)
                {
                    if (stringBuilder.ToString() == "Edit")
                    {
                        if (usernameTextBox == IntPtr.Zero)
                        {
                            usernameTextBox = handlePointer;
                        }
                        else
                        {
                            passwordTextBox = handlePointer;
                        }
                    }

                    if (stringBuilder.ToString() == "Button" && GetText(handlePointer) == "OK")
                    {
                        okButton = handlePointer;
                    }
                }
            }

            // Fill the username and password text boxes
            SendMessage(usernameTextBox, WM_SETTEXT, 0, _domainName + @"\" + _userName);
            SendMessage(passwordTextBox, WM_SETTEXT, 0, CryptoLibrary.CryptoLibrary.ToInsecureString(_password));

            // Click the OK button by simulating a left mouse button down & up event
            SendMessage(okButton, WM_LBUTTONDOWN, 0, 0);
            Thread.Sleep(100);
            SendMessage(okButton, WM_LBUTTONUP, 0, 0);
        }

        #endregion
    }
}
