using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;

namespace ListenerLibrary
{
    public class KeyboardListener
    {
        #region Imported DLLs and associated members

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc Ipfn, IntPtr mMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern int UnhookWindowsHookEx(IntPtr hhook);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhook, int code, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string IpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr IParam);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private LowLevelKeyboardProc _proc;
        private static IntPtr _hookID = IntPtr.Zero;

        #endregion

        public delegate void KeyboardPressed(KeyType key);
        public event KeyboardPressed OnKeyboardPressed;

        private string m_strFilePath = string.Empty;
        private bool m_bWriteTestFile = false;

        public KeyboardListener()
        {
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }

        /// <summary>
        /// Toggles whether the application writes to a file at the specified location. Use an empty string and false for toggling the test file off.
        /// Appends each global hook event's enumeration value as it's received.
        /// To fully test the library, press every key on the keyboard in sequence and verify the file has written as pressed.
        /// </summary>
        /// <param name="strFilePath"></param>
        public void ToggleTestFile(string strFilePath, bool bIsOn)
        {
            m_strFilePath = strFilePath;
            m_bWriteTestFile = bIsOn;
        }

        private void WriteToFile(KeyType key)
        {
            string strValue = string.Empty;
            
            try
            {
                using (StreamWriter sw = new StreamWriter(m_strFilePath, true))
                {
                    strValue = key.ToString();
                    strValue += " : Int Value = " + (int)key;

                    sw.WriteLine(strValue);
                }
            }
            catch
            {
                // Do nothing.
            }
        }

        private KeyType ParseKeyType(int nKey)
        {
            KeyType typeReturn = KeyType.Unknown;

            try
            {
                typeReturn = (KeyType)nKey;
                if (m_bWriteTestFile)
                    WriteToFile(typeReturn);
            }
            catch
            {
                // Leave this as unknown.
            }
            return typeReturn;
        }

        #region Global Hooking Methods

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            {
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr IParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(IParam);
                if(OnKeyboardPressed != null)
                    OnKeyboardPressed.Invoke(ParseKeyType(vkCode));
            }

            return CallNextHookEx(_hookID, nCode, wParam, IParam);
        }

        #endregion
    }
}
