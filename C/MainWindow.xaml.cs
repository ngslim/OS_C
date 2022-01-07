using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;



namespace C
{
    public partial class MainWindow : Window
    {
        String parentPassword = "admin";
        String childrenPassword = "children";
        bool hasScheduledThread;
        int failedCount = 0;
        List<Phase> phaseList;
        public MainWindow()
        {
            InitializeComponent();
            InstallRunOnStartUp();
            this.Topmost = true;
            txtPassword.Focus();
            Utils.CaptureScreen();
            phaseList = readPhaseList();
            if (phaseList.Count == 0)
            {
                displayAlertBox("Không có khung giờ nào được sử dụng", "Lỗi");
                return;
            }    
        }

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            handleLogin();
        }

        private void txtPassword_Submit(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Return)
            {
                handleLogin();
            }
        }

        private async void handleLogin()
        {
            if (txtPassword.SecurePassword.Length == 0)
            {
                //displayAlertBox("Chưa nhập mật khẩu");
                txtMessage.Text = "Chưa nhập mật khẩu";
            }
            else if (txtPassword.Password.ToString() == childrenPassword)
            {
                
                handlePostLogin();
            }
            else if (txtPassword.Password.ToString() == parentPassword)
            {
                if (hasScheduledThread)
                {
                    removeScheduledShutdown();
                    hasScheduledThread = false;
                }
                displayAlertBox($"Phụ huynh được dùng máy trong {Constant.PARENT_USE_DURATION / (60 * 1000)} phút", "Thông báo");
                hideWindow();
                await Task.Delay(Constant.PARENT_USE_DURATION);
                resetWindow();
            }
            else if (txtPassword.Password.ToString() == "close")
            {
                Environment.Exit(0);
            }
            else
            {
                txtMessage.Text = "Sai mật khẩu";
                txtPassword.Password = "";
                failedCount++;
                if (failedCount == Constant.MAX_FAILED_COUNT)
                {
                    hasScheduledThread = true;
                    Thread thread = new Thread(() => setMessage($"Bạn đã sai quá {Constant.MAX_FAILED_COUNT} lần, hệ thống sẽ tắt sau {Constant.WRONG_PASSWORD_SHUTDOWN/60} phút"));
                    thread.Start();
                    lockLogin();
                    setScheduledShutdown(Constant.WRONG_PASSWORD_SHUTDOWN);
                }
            }
        }

        private void hideWindow()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                Hide();
            }));
        }

        private void showWindow()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                Show();
            }));
        }

        private void resetWindow()
        {
            failedCount = 0;
            this.Dispatcher.Invoke(new Action(() =>
            {
                txtPassword.Focus();
                txtPassword.Password = "";
                txtMessage.Text = "Phiên đăng nhập đã hết, xin vui lòng đăng nhập lại";
                Show();
            }));
            this.WindowState = WindowState.Maximized;
            unlockLogin();
        }
        
        private void lockLogin()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                txtPassword.IsEnabled = false;
                btnSubmit.IsEnabled = false;
            }));
        }

        private void unlockLogin()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                txtPassword.IsEnabled = true;
                btnSubmit.IsEnabled = true;
            }));
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.LWin || e.Key == Key.RWin)
            {
                e.Handled = true;
                return;
            }
            if (e.Key == Key.System && e.SystemKey == Key.F4)
            {
                e.Handled = true;
            }
            if ((e.Key == Key.LeftAlt || e.Key == Key.RightAlt) && (e.Key == Key.LeftShift || e.Key == Key.RightShift) && e.Key == Key.Escape)
            {
                e.Handled = true;
            }
        }

        private void setScheduledShutdown(int seconds)
        {
            Process.Start("shutdown", $"/s /t {seconds}");
        }

        private void removeScheduledShutdown()
        {
            Process.Start("shutdown", "/a");
        }

        private List<Phase> readPhaseList()
        {
            List<Phase> phaseList = new List<Phase>();

            string[] lines = System.IO.File.ReadAllLines(@$"{Constant.APP_DIRECTORY}{Constant.CONFIG_FILE}");
            foreach (string line in lines)
            {
                Phase newPhase = new Phase(line);
                phaseList.Add(newPhase);
            }

            return phaseList;
        }

        private void handlePostLogin()
        {
            bool canUse = false;
            int phaseIndex = 0;

            phaseList = readPhaseList();

            foreach (Phase phase in phaseList)
            {
                if(phase.hasTime(Time.Now()))
                {
                    canUse = true;
                    break;
                }
                phaseIndex++;
            }
            
            if (canUse)
            {
                string title = $"Phiên sử dụng {phaseList[phaseIndex].From.ToString()} - {phaseList[phaseIndex].To.ToString()}";
                MessageBox.Show("Đăng nhập thành công!\n" + phaseList[phaseIndex].ToString(), title);

                handlePhase(Time.Now(), phaseIndex);
            }
            else
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    phaseIndex = -1;

                    for (int i = 0; i < phaseList.Count; i++)
                    {
                        if (phaseList[i].From > Time.Now())
                        {
                            if (phaseIndex == -1)
                            {
                                phaseIndex = i;
                            }
                            else
                            {
                                if (phaseList[i].From < phaseList[phaseIndex].From)
                                {
                                    phaseIndex = i;
                                }
                            }
                        }
                    }
                    if (phaseIndex == -1)
                    {
                        phaseIndex = 0;
                    }
                    txtMessage.Text = $"Không trong thời gian được sử dụng máy, thử lại vào {phaseList[phaseIndex].From}, máy sẽ tắt sau {Constant.WRONG_PHASE_SHUTDOWN} giây nếu phụ huynh không đăng nhập";
                    setScheduledShutdown(Constant.WRONG_PHASE_SHUTDOWN);
                    hasScheduledThread = true;
                }));
            }
        }
        private void InstallRunOnStartUp()
        {
            try
            {
                Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                Assembly curAssembly = Assembly.GetExecutingAssembly();
                key.SetValue(curAssembly.GetName().Name, curAssembly.Location);
            }
            catch { }
        }

        private async void handlePhase(Time startTime, int phaseIndex)
        {
            Phase phase = phaseList[phaseIndex];

            if (phase.Sum != 0)
            {
                displayAlertBox($"Hệ thống sẽ khoá vào {phase.To.ToString()} hoặc sau {phase.Sum.ToString()} phút sử dụng", "Thông báo");
            }
            else
            {
                displayAlertBox($"Hệ thống sẽ khoá vào {phase.To.ToString()}", "Thông báo");
            }

            int timeUsed = 0;
            int timeInPeriod = 0;

            hideWindow();
            lockLogin();

            while (true)
            {
                Utils.CaptureScreen();

                if (timeUsed >= phase.Sum || Time.Now() >= phase.To)
                {
                    showWindow();
                    setMessage($"Hệ thống sẽ mở khoá sau khi kết thúc phiên hoạt động vào {phase.To}");
                    Thread unlockLoginThread = new Thread(async () =>
                    {

                        await Task.Delay((phase.To - Time.Now()).ToSeconds() * 1000);
                        resetWindow();
                        return;
                    });
                    unlockLoginThread.Start();
                    return;
                }

                if (timeInPeriod == phase.Duration)
                {
                    showWindow();
                    setMessage($"Đã đến giờ nghỉ, bạn sẽ được tiếp tục sử dụng sau {phase.InterruptTime} phút");
                    await Task.Delay(phase.InterruptTime * 60 * 1000);
                    if (Time.Now()>=phase.To)
                    {
                        resetWindow();
                        return;
                    }
                    timeInPeriod = 0;
                    hideWindow();
                }

                if (timeUsed >= phase.Sum - 1 || Time.Now() == phase.To - new Time("00:01"))
                {
                    displayAlertBox($"Hệ thống sẽ khoá 1 phút nữa", "Thông báo");
                }

                await Task.Delay(Constant.REFRESH_RATE);
                int REFRESH_CODE = refreshPhase(ref phase, phaseIndex);
                if (REFRESH_CODE == 2)
                {
                    MessageBox.Show($"Phiên sử dụng đã bị xoá", "Thông báo");
                    resetWindow();
                    return;
                }
                if (REFRESH_CODE == 1)
                {
                    string title = $"Phiên sử dụng {phase.From.ToString()} - {phase.To.ToString()}";
                    MessageBox.Show("Phiên đã bị thay đổi\n" + phase.ToString(), title);
                }    
                timeUsed++;
                timeInPeriod++;
            }
        }

        private int refreshPhase(ref Phase phase, int phaseIndex)
        {
            phaseList = readPhaseList();
            if (phaseIndex >= phaseList.Count)
            {
                return 2;
            }
            
            foreach (Phase _phase in phaseList)
            {
                if (Time.Now() >= _phase.From && Time.Now() <= _phase.To)
                {
                    if(phase.From != _phase.From || phase.To != _phase.To || phase.Duration != _phase.Duration || phase.InterruptTime != _phase.InterruptTime || phase.Sum != _phase.Sum)
                    {
                        phase = _phase;
                        return 1; //Phase's changed
                    }
                    else
                    {
                        return 0; //Phase's unchanged
                    }
                }
            }

            return 2;
        }

        private void setMessage(String message)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                txtMessage.Text = message;
            }));
        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            Environment.Exit(0);
        }

        private void displayAlertBox(String message, String title)
        {
            Thread messageBoxThread = new Thread(() =>
            {
                MessageBox.Show(message, title);
            });
            messageBoxThread.Start();
        }
    }
}

