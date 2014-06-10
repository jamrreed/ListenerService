using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Timers;

namespace ListenerTest
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
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
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private static TcpListener listener;

        private static Dictionary<TcpClient, NetworkStream> m_dictClients = new Dictionary<TcpClient, NetworkStream>();

        public Window1()
        {
            InitializeComponent();

            _hookID = SetHook(_proc);
            InitializeTCPSockets();
        }

        private static void InitializeTCPSockets()
        {
            IPAddress address = IPAddress.Parse("127.0.0.1");
            listener = new TcpListener(address, 8000);
            listener.Start();
            StartAccept();

            Timer timerTestConnection = new Timer();
            timerTestConnection.Elapsed += Timer_Elapsed;
            timerTestConnection.Interval = 500;
            timerTestConnection.Start();
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (m_dictClients)
            {
                // Iterate through all of the clients to switch their status to disconnected if ping does not return.
                foreach (TcpClient client in m_dictClients.Keys)
                {
                    NetworkStream stream = m_dictClients[client];
                    if (stream == null)
                        continue;
                    if (stream.CanWrite)
                    {
                        try
                        {
                            stream.Write(new byte[] { 0x00 }, 0, 1); //Pings the client to see if its connected
                        }
                        catch
                        {
                            // Do nothing.
                        }
                    }
                }
            }
        }

        private static void StartAccept()
        {
            listener.BeginAcceptTcpClient(new AsyncCallback(SocketConnectionCallback), listener);
        }

        private static void SocketConnectionCallback(IAsyncResult result)
        {
            ClearDisconnectedClients();
            StartAccept();

            TcpClient client = listener.EndAcceptTcpClient(result);
            lock (m_dictClients)
            {
                if (!m_dictClients.ContainsKey(client))
                    m_dictClients.Add(client, client.GetStream());
            }
        }

        private static void ClearDisconnectedClients()
        {
            List<TcpClient> tempClients = new List<TcpClient>();
            lock (m_dictClients)
            {
                foreach (TcpClient client in m_dictClients.Keys)
                {
                    if (!client.Connected && !tempClients.Contains(client))
                        tempClients.Add(client);
                }

                foreach (TcpClient client in tempClients)
                {
                    client.Close();
                    m_dictClients.Remove(client);
                }
            }
        }

        private static void SetSocketData(int nValue)
        {
            lock (m_dictClients)
            {
                foreach (TcpClient client in m_dictClients.Keys)
                {
                    NetworkStream stream = m_dictClients[client];
                    if (stream == null)
                        continue;

                    if (stream.CanWrite)
                    {
                        byte bValue = 0x00;
                        try
                        {
                            bValue = Convert.ToByte(nValue);
                        }
                        catch
                        {

                        }
                        byte[] bytes = new byte[1];
                        bytes[0] = bValue;
                        try
                        {
                            stream.Write(bytes, 0, bytes.Length);
                        }
                        catch
                        {
                            // Do nothing.
                        }
                    }
                }
            }
        }

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

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr IParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(IParam);
                SetSocketData(vkCode);
            }

            return CallNextHookEx(_hookID, nCode, wParam, IParam);
        }
    }
}
