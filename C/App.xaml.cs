using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing.Imaging;
using System.Drawing;
using System.Windows.Forms;

namespace C
{
    static class Constant
    {
        public const int MAX_FAILED_COUNT = 3; //in seconds
        public const int TIME_BEFORE_USE = 3; //in seconds
        public const int PARENT_USE_DURATION = 60 * 60 * 1000; //60 minutes, in milliseconds
        public const int ONE_SECOND = 1000; //in miliseconds
        public const int WRONG_PASSWORD_SHUTDOWN = 10 * 60; //10 minutes, in seconds
        public const int WRONG_PHASE_SHUTDOWN = 15; //15 seconds
        public const int REFRESH_RATE = 1 * 60 * 1000; //1 minute, in milliseconds
        public const String APP_DIRECTORY = @"C:\Users\Public\Parental Control\";
        public const String SCREENSHOT_DIRECTORY = @"C:\Users\Public\Parental Control\Screenshots\";
        public const String CONFIG_FILE = "config.txt";
    }

    public class Time
    {
        public int Hour;
        public int Minute;

        public Time()
        {
            Hour = 0;
            Minute = 0;
        }

        public Time(int _hour, int _minute)
        {
            Hour = _hour;
            Minute = _minute;
        }

        public Time(string timeString) //format: hh:mm
        {
            string[] components = timeString.Split(':');
            Hour = Int32.Parse(components[0]);
            Minute = Int32.Parse(components[1]);
        }

        public static bool operator ==(Time a, Time b)
        {
            return a.Hour == b.Hour && a.Minute == b.Minute;
        }

        public static bool operator !=(Time a, Time b)
        {
            return a.Hour != b.Hour || a.Minute != b.Minute;
        }

        public static bool operator >(Time a, Time b)
        {
            return a.Hour > b.Hour || ((a.Hour == b.Hour) && (a.Minute > b.Minute));
        }

        public static bool operator <(Time a, Time b)
        {
            return a.Hour < b.Hour || ((a.Hour == b.Hour) && (a.Minute < b.Minute));
        }

        public static bool operator <=(Time a, Time b)
        {
            return a < b || a == b;
        }

        public static bool operator >=(Time a, Time b)
        {
            return a > b || a == b;
        }

        public static Time operator +(Time a, Time b)
        {
            Time sum = new Time();
            sum.Hour = a.Hour + b.Hour;
            sum.Hour += (a.Minute + b.Minute)/ 60;
            sum.Minute = (a.Minute + b.Minute) % 60;
            return sum;
        }

        public static Time operator -(Time a, Time b)
        {
            Time sub = new Time();
            sub.Hour = a.Hour - b.Hour;
            if(a.Minute >= b.Minute)
            {
                sub.Minute = a.Minute - b.Minute;
            } else
            {
                sub.Hour -= 1;
                sub.Minute = a.Minute - b.Minute + 60;
            }
            return sub;
        }

        public static Time Now()
        {
            DateTime dateTime = DateTime.Now;
            return new Time(dateTime.Hour, dateTime.Minute);
        }

        public int ToSeconds()
        {
            return Hour * 3600 + Minute * 60;
        }

        public int ToMinutes()
        {
            return Hour * 60 + Minute;
        }

        public static Time MinuteToTime(int minute)
        {
            return new Time(minute/60, minute%60);
        }

        public override string ToString()
        {
            string timeString = "";
            if (Hour < 10)
            {
                timeString += "0" + Hour.ToString();
            }
            else
            {
                timeString += Hour.ToString();
            }
            timeString += ":";
            if (Minute < 10)
            {
                timeString += "0" + Minute.ToString();
            }
            else
            {
                timeString += Minute.ToString();
            }
            return timeString;
        }
    }

    public class Phase
    {
        public Time From = new Time();
        public Time To = new Time();
        public int Duration = 0;
        public int InterruptTime = 0;
        public int Sum = 0;

        public Phase(string phaseString)
        {
            string[] components = phaseString.Split(' ');
            foreach (string component in components)
            {
                char index = component[0];
                string value = component.Substring(1);
                if (index == 'F')
                {
                    From = new Time(value); 
                }
                if (index == 'T')
                {
                    To = new Time(value);
                }
                if (index == 'D')
                {
                    Duration = Int32.Parse(value);
                }
                if (index == 'I')
                {
                    InterruptTime = Int32.Parse(value);
                }
                if (index == 'S')
                {
                    Sum = Int32.Parse(value);
                }
            }
        }

        public override string ToString()
        {
            string phaseString = "";
            if (Duration != 0)
            {
                phaseString += "\nThời gian mỗi quãng: " + Duration.ToString() + " phút";
            }
            if (InterruptTime != 0)
            {
                phaseString += "\nThời gian nghỉ giữa quãng: " + InterruptTime.ToString() + " phút";
            }
            if (Sum != 0)
            {
                phaseString += "\nTổng thời gian được sử dụng: " + Sum.ToString() + " phút";
            }
            return phaseString;
        }

        public bool hasTime(Time time)
        {   
            if (time >= From && time <= To)
            {
                return true;
            }
            return false;
        }
    }

    public static class Utils
    {
        public static void CaptureScreen()
        {
            Bitmap captureBitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, PixelFormat.Format32bppArgb);
            Rectangle captureRectangle = Screen.AllScreens[0].Bounds;
            Graphics captureGraphics = Graphics.FromImage(captureBitmap);
            captureGraphics.CopyFromScreen(captureRectangle.Left, captureRectangle.Top, 0, 0, captureRectangle.Size);
            DateTime now = DateTime.Now;
            string fileName = $"Screenshot_{now.Day.ToString()}_{now.Month.ToString()}_{now.Year.ToString()}_{now.Hour.ToString()}h{now.Minute.ToString()}m{now.Second.ToString()}s";
            captureBitmap.Save(@$"{Constant.SCREENSHOT_DIRECTORY}{fileName}.jpg", ImageFormat.Jpeg);
        }
    }

    public partial class App : System.Windows.Application
    {

    }
}
