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
using System.Timers;
using System.Net;
using System.Net.Sockets;
using System.IO;
using ListenerLibrary;

namespace Timers
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const double TIMER_DURATION = 100; // in Milliseconds

        private Timer m_timer1 = new Timer();
        private Timer m_timer2 = new Timer();
        private Timer m_timer3 = new Timer();
        private Timer m_timer4 = new Timer();
        private Timer m_timer5 = new Timer();
        private Timer m_timer6 = new Timer();

        private Dictionary<Timer, TextBlock> m_dictTimerTB = new Dictionary<Timer, TextBlock>();
        private Dictionary<Button, double> m_dictButtonTimerValues = new Dictionary<Button, double>();
        private Dictionary<Button, bool> m_dictButtonClicked = new Dictionary<Button, bool>();
        private Dictionary<Button, Timer> m_dictButtonTimers = new Dictionary<Button, Timer>();

        private KeyboardListener m_kbListener = new KeyboardListener();

        public MainWindow()
        {
            InitializeComponent(); 
            InitializeTimers(); 
            InitializeAssociation();

            // Event that listens to the global hook to receive keystrokes while the application is not focused.
            m_kbListener.OnKeyboardPressed += m_kbListener_OnKeyboardPressed;
        }

        /// <summary>
        /// Event handler that subscribes to the global hook driven event in the KeyboardListener Dll wrapper.
        /// </summary>
        /// <param name="key"></param>
        private void m_kbListener_OnKeyboardPressed(KeyType key)
        {
            ProcessKeyboardValue(key);
        }

        private void InitializeTimers()
        {
            m_timer1.Interval = TIMER_DURATION;
            m_timer2.Interval = TIMER_DURATION;
            m_timer3.Interval = TIMER_DURATION;
            m_timer4.Interval = TIMER_DURATION;
            m_timer5.Interval = TIMER_DURATION;
            m_timer6.Interval = TIMER_DURATION;

            m_timer1.Elapsed += m_timer_Elapsed;
            m_timer2.Elapsed += m_timer_Elapsed;
            m_timer3.Elapsed += m_timer_Elapsed;
            m_timer4.Elapsed += m_timer_Elapsed;
            m_timer5.Elapsed += m_timer_Elapsed;
            m_timer6.Elapsed += m_timer_Elapsed;
        }

        private void InitializeAssociation()
        {
            // Associate timers with textblocks
            m_dictTimerTB.Clear(); 
            m_dictTimerTB.Add(m_timer1, tb1);
            m_dictTimerTB.Add(m_timer2, tb2);
            m_dictTimerTB.Add(m_timer3, tb3);
            m_dictTimerTB.Add(m_timer4, tb4);
            m_dictTimerTB.Add(m_timer5, tb5);
            m_dictTimerTB.Add(m_timer6, tb6);

            m_dictButtonTimers.Clear();
            m_dictButtonTimers.Add(btn1, m_timer1);
            m_dictButtonTimers.Add(btn2, m_timer2);
            m_dictButtonTimers.Add(btn3, m_timer3);
            m_dictButtonTimers.Add(btn4, m_timer4);
            m_dictButtonTimers.Add(btn5, m_timer5);
            m_dictButtonTimers.Add(btn6, m_timer6);

            // Associate buttons with starting values of countdown 
            m_dictButtonTimerValues.Clear();
            m_dictButtonTimerValues.Add(btn1, 420.0);
            m_dictButtonTimerValues.Add(btn2, 360.0);
            m_dictButtonTimerValues.Add(btn3, 300.0);
            m_dictButtonTimerValues.Add(btn4, 300.0);
            m_dictButtonTimerValues.Add(btn5, 300.0);
            m_dictButtonTimerValues.Add(btn6, 300.0);

            // Associate buttons with boolean values to indicate if timers are active
            m_dictButtonClicked.Clear();
            m_dictButtonClicked.Add(btn1, false);
            m_dictButtonClicked.Add(btn2, false);
            m_dictButtonClicked.Add(btn3, false);
            m_dictButtonClicked.Add(btn4, false);
            m_dictButtonClicked.Add(btn5, false);
            m_dictButtonClicked.Add(btn6, false);
        }

        private void ProcessTimerElapsed(Timer sender)
        {
            if (sender == null || !m_dictTimerTB.ContainsKey(sender))
                return;

            Dispatcher.Invoke(new Action(delegate
                {
                    TextBlock tb = m_dictTimerTB[sender];
                    if (tb == null)
                        return;

                    double dblValue = 0;
                    if (double.TryParse(tb.Text, out dblValue))
                    {
                        if (dblValue <= 0)
                        {
                            sender.Stop();
                            tb.Text = "Ready";
                            Button button = (from Button btn in m_dictButtonTimers.Keys
                                             where m_dictButtonTimers[btn] == sender
                                             select btn).FirstOrDefault();
                            if (button != null)
                                button.BorderBrush = Brushes.GreenYellow; // Indicate we're ready for the next timer.
                        }
                        else
                        {
                            double delta = TIMER_DURATION / 1000;
                            dblValue = dblValue - delta;
                            tb.Text = dblValue.ToString("F1"); // Formats to fixed point of 1 decimal place.
                        }
                    }
                }));
        }

        private void ProcessButtonClicked(Button sender)
        {
            if (sender == null || !m_dictButtonClicked.ContainsKey(sender) || 
                !m_dictButtonTimerValues.ContainsKey(sender) || !m_dictButtonTimers.ContainsKey(sender))
                return;

            Timer timer = m_dictButtonTimers[sender]; if (timer == null) return;
            TextBlock tb = m_dictTimerTB[timer]; if (tb == null) return;
            double dblValue = m_dictButtonTimerValues[sender];
            bool bIsClicked = !m_dictButtonClicked[sender];

            m_dictButtonClicked[sender] = bIsClicked; // Alternate the active state
            if (bIsClicked)
            {
                timer.Start();
                sender.BorderBrush = Brushes.Tomato;
                tb.Text = dblValue.ToString();
            }
            else
            {
                timer.Stop();
                sender.BorderBrush = Brushes.GreenYellow;
                tb.Text = "Ready";
            }
        }

        void m_timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!(sender is Timer))
                return;

            ProcessTimerElapsed(sender as Timer);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button))
                return;

            ProcessButtonClicked((Button)sender);
        }

        private void ProcessKeyboardValue(KeyType type)
        {
            int nButton = 0;
            switch (type)
            {
                case KeyType.NumPad1: //1 bot left
                    nButton = 5;
                    break;
                case KeyType.NumPad3: //3 bot right
                    nButton = 6;
                    break;
                case KeyType.NumPad4: //4 mid left
                    nButton = 3;
                    break;
                case KeyType.NumPad6: //6 mid right
                    nButton = 4;
                    break;
                case KeyType.NumPad7: //7 top left
                    nButton = 1;
                    break;
                case KeyType.NumPad9: //9 top right
                    nButton = 2;
                    break;
                default:
                    return;
            }

            Dispatcher.Invoke(new Action(delegate
            {
                object ctrl = FindName("btn" + nButton);
                if (ctrl != null && ctrl is Button)
                    ProcessButtonClicked((Button)ctrl);
            }));
        }
    }
}
