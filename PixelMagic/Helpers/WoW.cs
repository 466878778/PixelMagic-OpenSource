﻿//////////////////////////////////////////////////
//                                              //
//   See License.txt for Licensing information  //
//                                              //
//////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.DirectoryServices.AccountManagement;
using System.Text;

namespace PixelMagic.Helpers
{
    public static class WoW
    {
        internal static Process pWow = null;
        internal static Random random;

        public static string Version
        {
            get
            {
                return pWow.MainModule.FileVersionInfo.FileVersion;
            }
        }

        public static string InstallPath
        {
            get
            {
                string wowFolder = ConfigFile.ReadValue<string>("PixelBot", "WoWPath").Trim();

                if (wowFolder == "")
                {
                    Log.Write("Finding WoW Install Path...");

                    wowFolder = RegEdit.HKLMReadKey(@"Software\Wow6432Node\Blizzard Entertainment\World of Warcraft", "InstallPath");
                    ConfigFile.WriteValue("PixelBot", "WoWPath", wowFolder);
                }

                return wowFolder;
            }
        }

        public static string AddonPath
        {
            get
            {
                return InstallPath + "\\Interface\\AddOns";
            }
        }

        private static bool LimitedUserExists
        {
            get
            {
                using (PrincipalContext pc = new PrincipalContext(ContextType.Machine))
                {
                    UserPrincipal up = UserPrincipal.FindByIdentity(pc, IdentityType.SamAccountName, "Limited");
                    return (up != null);
                }
            }
        }

        public static void Initialize()
        {
            Log.Write("Searching for open WoW processes...");

            random = new Random();

            pWow = Process.GetProcessesByName("Wow").FirstOrDefault();
            if (pWow == null)
            {
                pWow = Process.GetProcessesByName("Wow-64").FirstOrDefault();
            }
            if (pWow == null)
            {
                pWow = Process.GetProcessesByName("WowB-64").FirstOrDefault();
            }

            if (pWow == null)
            {
                Log.Write("Failed to find open wow process, please ensure that x64 WoW is running.", Color.Red);

                DialogResult res = MessageBox.Show("WoW is not running, would you like to launch it now?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (res == DialogResult.Yes)
                {
                    string wowFolder = ConfigFile.ReadValue<string>("PixelBot", "WoWPath").Trim();
                    ProcessStartInfo psi;
                    
                    if (LimitedUserExists)  // When running on my PC I like to launch wow as a limited user so that it does not have access to tasklist to                     
                    {                       // See details of running tasks - it gets access denied messages instead
                                            // This is just my personal extra layer of anti-warden protection, but most people will say its not needed.

                        // The command we run is
                        // C:\Windows\System32\runas.exe /user:Limited /savecred /env "C:\Games\World of Warcraft Live\Wow.exe"

                        psi = new ProcessStartInfo(@"C:\Windows\System32\runas.exe")
                        {
                            Arguments =  $"/user:Limited /savecred /env \"{wowFolder}\\Wow-64.exe\""
                        };                       
                    }
                    else
                    {
                        psi = new ProcessStartInfo(wowFolder + "\\Wow-64.exe");
                    }

                    psi.WorkingDirectory = wowFolder;

                    pWow = Process.Start(psi);

                    if (LimitedUserExists)
                    {
                        bool running = false;

                        while (!running)
                        {
                            pWow = Process.GetProcessesByName("Wow-64").FirstOrDefault();
                            if (pWow != null)
                            {
                                running = true;
                            }
                        }
                    }
                    else
                    {
                        pWow.WaitForInputIdle();
                    }

                    Log.Write("Successfully launched WoW with process ID: " + pWow.Id);
                }
                else
                {
                    return;
                }
            }

            Log.Write("Connecting to process with ID: " + pWow.Id);
            Log.Write("Successfully connected.", Color.Green);

            bool is64 = (pWow.ProcessName.Contains("64"));
            
            Log.Write($"WoW Version: {Version} (x{(is64 ? "64" : "86")})");
        }

        public static void Dispose()
        {
            Log.Write("Disposing of WoW Process...");
            pWow.Close();
            pWow = null;
            Log.Write("Disposing of WoW Process Completed.");
        }

        public static bool HasTarget
        {
            get
            {
                Color c = GetBlockColor(2, 3);
                return ((c.R == Color.Red.R) && (c.G == Color.Red.G) && (c.B == Color.Red.B));
            }
        }

        public static bool PlayerIsCasting
        {
            get
            {
                Color c = GetBlockColor(3, 3);
                return ((c.R == Color.Red.R) && (c.G == Color.Red.G) && (c.B == Color.Red.B));
            }
        }

        public static bool TargetIsCasting
        {
            get
            {
                Color c = GetBlockColor(4, 3);
                return ((c.R == Color.Red.R) && (c.G == Color.Red.G) && (c.B == Color.Red.B));
            }
        }

        public static bool TargetIsFriend
        {
            get
            {
                Color c = GetBlockColor(1, 3);                
                return ((c.R == 0) && (c.G == 255) && (c.B == 0));
            }
        }

        public static int HealthPercent
        {
            get
            {
                // First we build up the binary string that makes up health
                // This is read from the 2nd row on the screen of pixel information
                // It is displayed as binary, so 100% health = 1100100
                string binaryHealth = "";

                for (int x = 1; x <= 7; x++)
                {
                    Color c = GetBlockColor(x, 2);
                    binaryHealth += ((c.R == Color.Red.R) && (c.G == Color.Red.G) && (c.B == Color.Red.B)) ? "1" : "0";
                }

                return Convert.ToInt32(binaryHealth, 2);
            }
        }

        public static int TargetHealthPercent
        {
            get
            {
                // First we build up the binary string that makes up health
                // This is read from the 2nd row on the screen of pixel information
                // It is displayed as binary, so 100% health = 1100100
                string binaryHealth = "";

                for (int x = 1; x <= 7; x++)
                {
                    Color c = GetBlockColor(x, 5);
                    binaryHealth += ((c.R == Color.Red.R) && (c.G == Color.Red.G) && (c.B == Color.Red.B)) ? "1" : "0";
                }

                return Convert.ToInt32(binaryHealth, 2);
            }
        }

        public static int Power
        {
            get
            {
                // First we build up the binary string that makes up power
                // This is read from the 4th row on the screen of pixel information
                // It is displayed as binary, so 100 power = 1100100
                string binaryPower = "";

                for (int x = 1; x <= 7; x++)
                {
                    Color c = GetBlockColor(x, 4);
                    binaryPower += ((c.R == Color.Red.R) && (c.G == Color.Red.G) && (c.B == Color.Red.B)) ? "1" : "0";
                }

                return Convert.ToInt32(binaryPower, 2);
            }
        }
        public static int Focus
        {
            get
            {
                return Power;
            }
        }
        public static int Mana
        {
            get
            {
                return Power;
            }
        }
        public static int Energy
        {
            get
            {
                return Power;
            }
        }
        public static int Rage
        {
            get
            {
                return Power;
            }
        }

        public static bool IsSpellOnCooldown(int spellNoInArrayOfSpells)  // This will take the spell no from the array of spells, 1, 2, 3 ..... n
        {
            Color c = GetBlockColor(5 + spellNoInArrayOfSpells, 1);
            return ((c.R == Color.Red.R) && (c.G == Color.Red.G) && (c.B == Color.Red.B));
        }

        public static bool IsSpellInRange(int spellNoInArrayOfSpells)  // This will take the spell no from the array of spells, 1, 2, 3 ..... n
        {
            Color c = GetBlockColor(spellNoInArrayOfSpells, 6);
            return ((c.R == Color.Red.R) && (c.G == Color.Red.G) && (c.B == Color.Red.B));
        }

        public static bool CanCast(int spellNoInArrayOfSpells)
        {
            if (IsSpellOnCooldown(spellNoInArrayOfSpells) == true)
            {
                //Log.Write("Spell No: " + spellNoInArrayOfSpells + " is on CD...", Color.Red);
                return false;
            }

            if (IsSpellInRange(spellNoInArrayOfSpells) == false)
            {
                //Log.Write("Spell No: " + spellNoInArrayOfSpells + " is not in range...", Color.Red);
                return false;
            }

            //Log.Write("Spell No: " + spellNoInArrayOfSpells + " is in range and not on CD...", Color.Green);
            return true;
        }

        public static void SendKey(Keys key, int milliseconds = 50)
        {
            Log.Write("Sending keypress: " + key, Color.Gray);

            if (milliseconds < 50)
                milliseconds = 50;

            milliseconds = milliseconds + random.Next(50);

            KeyDown(key);
            Thread.Sleep(milliseconds);
            KeyUp(key);
        }

        public static void SendKeyAtLocation(Keys key, int x, int y)
        {
            Log.Write($"Sending keypress {key} at location: x = {x}, y = {y}", Color.Gray);
                        
            KeyDown(key);
            Thread.Sleep(50);
            KeyUp(key);
          
            Mouse.LeftClick(x, y);            
        }

        //public static void SendKeyAtMe(Keys key)
        //{


        //    Log.Write($"Sending keypress {key} at location: x = {x}, y = {y}", Color.Gray);

        //    KeyDown(key);
        //    Thread.Sleep(50);

        //    KeyUp(key);
        //    Thread.Sleep(300);

        //    Mouse.LeftClick(x, y);
        //}

        public static void SendMacro(string macro)
        {
            Log.Write("Sending macro: " + macro, Color.Gray);

            KeyPressRelease(Keys.Enter);
            Thread.Sleep(100);
            Write(macro);
            Thread.Sleep(100);
            KeyPressRelease(Keys.Enter);
        }

        private static object thisLock = new object();

        [DllImport("gdi32.dll")]
        private static extern int BitBlt(IntPtr srchDC, int srcX, int srcY, int srcW, int srcH, IntPtr desthDC, int destX, int destY, int op);

        static Bitmap screenPixel = new Bitmap(1, 1);

        // This is apparently one of the fastest ways to read single pixel color
        // http://stackoverflow.com/questions/17130138/fastest-way-to-get-screen-pixel-color-in-c-sharp

        public static Color GetBlockColor(int column, int row)
        {
            if (pWow == null)
                return Color.Black;

            if ((column <= 0) || (row <= 0))
                throw new Exception("x and or y must be >= 1");

            column = ((column - 1) * 7);  // For some unknown reason pixel size of 5x5 in WoW = 7x7 in C#
            row = ((row - 1) * 7);
           
            lock (thisLock)  // We lock the bitmap "screenPixel" here to avoid it from being accessed by multiple threads at the same time and crashing
            {
                try
                {
                    using (Graphics gdest = Graphics.FromImage(screenPixel))
                    {
                        using (Graphics gsrc = Graphics.FromHwnd(pWow.MainWindowHandle))
                        {
                            IntPtr hSrcDC = gsrc.GetHdc();
                            IntPtr hDC = gdest.GetHdc();
                            int retval = BitBlt(hDC, 0, 0, 1, 1, hSrcDC, column, row, (int)CopyPixelOperation.SourceCopy);
                            gdest.ReleaseHdc();
                            gsrc.ReleaseHdc();
                        }
                    }
                    Color temp = screenPixel.GetPixel(0, 0);

                    return temp;
                }
                catch (Exception ex)
                {
                    Log.Write("Failed to find pixel color from screen, this is usually due to wow closing while", Color.Red);
                    Log.Write("attempting to find the pixel color", Color.Red);
                    Log.Write("Error Details: " + ex.Message, Color.Red);

                    throw ex;
                }
            }
        }

        //public Color GetPixelColorAt(int x, int y)
        //{
        //    if (pWow == null)
        //        return Color.Black;

        //    using (Graphics gdest = Graphics.FromImage(screenPixel))
        //    {
        //        using (Graphics gsrc = Graphics.FromHwnd(pWow.MainWindowHandle))
        //        {
        //            IntPtr hSrcDC = gsrc.GetHdc();
        //            IntPtr hDC = gdest.GetHdc();
        //            int retval = BitBlt(hDC, 0, 0, 1, 1, hSrcDC, x, y, (int)CopyPixelOperation.SourceCopy);
        //            gdest.ReleaseHdc();
        //            gsrc.ReleaseHdc();
        //        }
        //    }
        //    Color temp = screenPixel.GetPixel(0, 0);

        //    if ((temp.R == Color.White.R) && (temp.G == Color.White.G) && (temp.B == Color.White.B))
        //    {
        //        Log.Write($"Color @ (x,y) = ({x},{y}) = {temp.ToString()}", Color.Black);
        //    }
        //    else
        //    {
        //        Log.Write($"Color @ (x,y) = ({x},{y}) = {temp.ToString()}", temp);
        //    }

        //    return temp;
        //}

        //public class Status
        //{
        //    public enum GameState
        //    {
        //        InGame,
        //        NotInGame
        //    }
        //}

        //public Status.GameState GameState
        //{
        //    get
        //    {
        //        if (wow.Read<byte>(Offsets.GameState) == 1)
        //        {
        //            return Status.GameState.InGame;
        //        }
        //        else
        //        {
        //            return Status.GameState.NotInGame;
        //        }
        //    }
        //}

        #region Keyboard Input
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, UIntPtr wParam, UIntPtr lParam);

        private static void KeyDown(Keys Key)
        {
            SendMessage(pWow.MainWindowHandle, 0x100, (int)Key, 0);
        }

        private static void KeyUp(Keys Key)
        {
            SendMessage(pWow.MainWindowHandle, 0x101, (int)Key, 0);
        }

        private static void KeyPressRelease(Keys key)
        {
            KeyDown(key);
            Thread.Sleep(50);
            KeyUp(key);
        }

        private static void Write(string text, params object[] args)
        {
            foreach (char character in string.Format(text, args))
            {
                PostMessage(pWow.MainWindowHandle, 0x0102, new UIntPtr(character), UIntPtr.Zero);
            }
        }
        #endregion

        public static bool HasFocus
        {
            get
            {
                var activatedHandle = GetForegroundWindow();
                if (activatedHandle == IntPtr.Zero)
                {
                    return false;       // No window is currently activated
                }

                int activeProcId;
                GetWindowThreadProcessId(activatedHandle, out activeProcId);

                if (pWow == null)
                {
                    throw new Exception("World of warcraft is not detected / running, please login before attempting to restart the bot");
                }

                return activeProcId == pWow.Id;
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        public static string GetText(IntPtr hWnd)
        {
            // Allocate correct string length first
            int length = GetWindowTextLength(hWnd);
            StringBuilder sb = new StringBuilder(length + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }
        
        [Flags]
        public enum Keys
        {
            A = 0x41,
            Add = 0x6b,
            Alt = 0x40000,
            Apps = 0x5d,
            Attn = 0xf6,
            B = 0x42,
            Back = 8,
            BrowserBack = 0xa6,
            BrowserFavorites = 0xab,
            BrowserForward = 0xa7,
            BrowserHome = 0xac,
            BrowserRefresh = 0xa8,
            BrowserSearch = 170,
            BrowserStop = 0xa9,
            C = 0x43,
            Cancel = 3,
            Capital = 20,
            CapsLock = 20,
            Clear = 12,
            Control = 0x20000,
            ControlKey = 0x11,
            Crsel = 0xf7,
            D = 0x44,
            D0 = 0x30,
            D1 = 0x31,
            D2 = 50,
            D3 = 0x33,
            D4 = 0x34,
            D5 = 0x35,
            D6 = 0x36,
            D7 = 0x37,
            D8 = 0x38,
            D9 = 0x39,
            Decimal = 110,
            Delete = 0x2e,
            Divide = 0x6f,
            Down = 40,
            E = 0x45,
            End = 0x23,
            Enter = 13,
            EraseEof = 0xf9,
            Escape = 0x1b,
            Execute = 0x2b,
            Exsel = 0xf8,
            F = 70,
            F1 = 0x70,
            F10 = 0x79,
            F11 = 0x7a,
            F12 = 0x7b,
            F13 = 0x7c,
            F14 = 0x7d,
            F15 = 0x7e,
            F16 = 0x7f,
            F17 = 0x80,
            F18 = 0x81,
            F19 = 130,
            F2 = 0x71,
            F20 = 0x83,
            F21 = 0x84,
            F22 = 0x85,
            F23 = 0x86,
            F24 = 0x87,
            F3 = 0x72,
            F4 = 0x73,
            F5 = 0x74,
            F6 = 0x75,
            F7 = 0x76,
            F8 = 0x77,
            F9 = 120,
            FinalMode = 0x18,
            G = 0x47,
            H = 0x48,
            HanguelMode = 0x15,
            HangulMode = 0x15,
            HanjaMode = 0x19,
            Help = 0x2f,
            Home = 0x24,
            I = 0x49,
            ImeAccept = 30,
            ImeAceept = 30,
            ImeConvert = 0x1c,
            ImeModeChange = 0x1f,
            ImeNonconvert = 0x1d,
            Insert = 0x2d,
            J = 0x4a,
            JunjaMode = 0x17,
            K = 0x4b,
            KanaMode = 0x15,
            KanjiMode = 0x19,
            KeyCode = 0xffff,
            L = 0x4c,
            LaunchApplication1 = 0xb6,
            LaunchApplication2 = 0xb7,
            LaunchMail = 180,
            LButton = 1,
            LControlKey = 0xa2,
            Left = 0x25,
            LineFeed = 10,
            LMenu = 0xa4,
            LShiftKey = 160,
            LWin = 0x5b,
            M = 0x4d,
            MButton = 4,
            MediaNextTrack = 0xb0,
            MediaPlayPause = 0xb3,
            MediaPreviousTrack = 0xb1,
            MediaStop = 0xb2,
            Menu = 0x12,
            Modifiers = -65536,
            Multiply = 0x6a,
            N = 0x4e,
            Next = 0x22,
            NoName = 0xfc,
            None = 0,
            NumLock = 0x90,
            NumPad0 = 0x60,
            NumPad1 = 0x61,
            NumPad2 = 0x62,
            NumPad3 = 0x63,
            NumPad4 = 100,
            NumPad5 = 0x65,
            NumPad6 = 0x66,
            NumPad7 = 0x67,
            NumPad8 = 0x68,
            NumPad9 = 0x69,
            O = 0x4f,
            Oem1 = 0xba,
            Oem102 = 0xe2,
            Oem2 = 0xbf,
            Oem3 = 0xc0,
            Oem4 = 0xdb,
            Oem5 = 220,
            Oem6 = 0xdd,
            Oem7 = 0xde,
            Oem8 = 0xdf,
            OemBackslash = 0xe2,
            OemClear = 0xfe,
            OemCloseBrackets = 0xdd,
            Oemcomma = 0xbc,
            OemMinus = 0xbd,
            OemOpenBrackets = 0xdb,
            OemPeriod = 190,
            OemPipe = 220,
            Oemplus = 0xbb,
            OemQuestion = 0xbf,
            OemQuotes = 0xde,
            OemSemicolon = 0xba,
            Oemtilde = 0xc0,
            P = 80,
            Pa1 = 0xfd,
            Packet = 0xe7,
            PageDown = 0x22,
            PageUp = 0x21,
            Pause = 0x13,
            Play = 250,
            Print = 0x2a,
            PrintScreen = 0x2c,
            Prior = 0x21,
            ProcessKey = 0xe5,
            Q = 0x51,
            R = 0x52,
            RButton = 2,
            RControlKey = 0xa3,
            Return = 13,
            Right = 0x27,
            RMenu = 0xa5,
            RShiftKey = 0xa1,
            RWin = 0x5c,
            S = 0x53,
            Scroll = 0x91,
            Select = 0x29,
            SelectMedia = 0xb5,
            Separator = 0x6c,
            Shift = 0x10000,
            ShiftKey = 0x10,
            Sleep = 0x5f,
            Snapshot = 0x2c,
            Space = 0x20,
            Subtract = 0x6d,
            T = 0x54,
            Tab = 9,
            U = 0x55,
            Up = 0x26,
            V = 0x56,
            VolumeDown = 0xae,
            VolumeMute = 0xad,
            VolumeUp = 0xaf,
            W = 0x57,
            X = 0x58,
            XButton1 = 5,
            XButton2 = 6,
            Y = 0x59,
            Z = 90,
            Zoom = 0xfb
        }
    }

}