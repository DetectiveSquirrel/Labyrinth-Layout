using PoeHUD.Controllers;
using PoeHUD.Plugins;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace Labyrinth_Layout
{
    public class PluginCore : BaseSettingsPlugin<PluginSettings>
    {

        public string poe_lab_url = "http://www.poelab.com/";
        public string poe_lab_diff;
        public string poe_lab_diff_url = "";
        public string CurrentFileName = "layout";

        public bool ImageReady = false;
        public bool ChangingImage = false;
        public bool Started = false;
        public bool InLabyrinth = false;
        public bool AutoSelectedDiff = false;
        public bool FightingIzaro = false;
        public bool ManualHide = false;
        public bool isHideKeyDown = false;
        public bool isReloadKeyDown = false;
        public string NewSelectedDiff;

        public override void Initialise()
        {
            Settings.Difficulty.OnValueSelected += SetNewDifficulty;
            GameController.Area.OnAreaChange += OnAreaChange;

            List<string> diffs = new List<string>() { "Uber", "Merciless", "Cruel", "Normal" };
            Settings.Difficulty.SetListValues(diffs);
            poe_lab_diff = diffs[0];

            // display default league in setting
            if (Settings.Difficulty.Value == null)
                Settings.Difficulty.Value = poe_lab_diff;


            // set wanted league
            poe_lab_diff = Settings.Difficulty.Value.ToLower();

            if (Settings.LoadOnStart)
                Started = true;
        }

        private void Reload()
        {
            Started = true;
            ImageReady = false;
        }

        private void SetNewDifficulty(string value)
        {
            ChangingImage = true;
            NewSelectedDiff = value;
        }

        public PluginCore()
        {
            PluginName = "Labyrinth Overlay";
        }

        private void OnAreaChange(AreaController area)
        {
            //File.WriteAllText(@"D:\Path of exile Tools\PoE HUD (NEW)\Release\plugins\Labyrinth-Layout\WriteLines.txt", area.CurrentArea.Name);

            if (InLabyrinth)
            {

                switch (area.CurrentArea.RealLevel)
                {
                    case 33:
                        poe_lab_diff = "normal";
                        break;
                    case 55:
                        poe_lab_diff = "cruel";
                        break;
                    case 68:
                        poe_lab_diff = "merciless";
                        break;
                    case 75:
                        poe_lab_diff = "uber";
                        break;
                }

                if (!AutoSelectedDiff && Settings.OnlyLabyrinth)
                {
                    AutoSelectedDiff = true;
                    ImageReady = false;
                    if (Settings.LoadOnStart)
                        Started = true;
                }
            }

            // reset every area just toi be safe
            FightingIzaro = false;

            // dont show in izaro room there is no need to hide hp bar.
            if (area.CurrentArea.Name == "Aspirants' Plaza")
                InLabyrinth = true;

            if (area.CurrentArea.Name == "Aspirant's Trial")
                FightingIzaro = true;

            if (area.CurrentArea.IsTown || area.CurrentArea.IsHideout)
            {
                InLabyrinth = false;
                AutoSelectedDiff = false;
            }
        }

        public override void Render()
        {
            base.Render();
            if (Keyboard.IsKeyDown((int)Settings.HotKey.Value) && !isReloadKeyDown)
            {
                isReloadKeyDown = true;
                Started = true;
                ImageReady = false;
            }
            if (!Keyboard.IsKeyDown((int)Settings.HotKey.Value) && isReloadKeyDown)
            {
                isReloadKeyDown = false;
            }
            if (Keyboard.IsKeyDown((int)Settings.ManualToggle.Value) && !isHideKeyDown)
            {
                isHideKeyDown = true;
                if (isHideKeyDown)
                    ManualHide = !ManualHide;
            }
            if (!Keyboard.IsKeyDown((int)Settings.ManualToggle.Value) && isHideKeyDown)
            {
                isHideKeyDown = false;
            }

            if (ChangingImage)
            {
                poe_lab_diff = NewSelectedDiff.ToLower();
                ImageReady = false;
                ChangingImage = false;
            }

            if (Started)
            {
                if (!ImageReady)
                {
                    ClearImages();

                    GetRightPage();
                    SaveImage();
                    ImageReady = true;
                }
                else
                {
                    if (Settings.OnlyLabyrinth)
                    {
                        if (InLabyrinth && AutoSelectedDiff && !FightingIzaro)
                        {
                            if (!ManualHide)
                                DrawLayoutImage();
                        }
                    }
                    else
                    {
                        if (!ManualHide)
                            DrawLayoutImage();
                    }
                }
            }
        }

        private void ClearImages()
        {
            string[] dirs = Directory.GetFiles(PluginDirectory + $"\\images\\");
            foreach (string dir in dirs)
            {
                File.Delete(dir);
            }
        }

        private void DrawLayoutImage()
        {
            double PercentChange = Settings.Size / (double)100;
            int Width = ChangeByPercent(PercentChange, 841);
            int Height = ChangeByPercent(PercentChange, 270);
            float X = Settings.X - (Width / 2);
            float Y = Settings.Y;
            Graphics.DrawPluginImage(PluginDirectory + $"\\images\\{CurrentFileName}.png", new SharpDX.RectangleF(X, Y, Width, Height));
        }

        private int ChangeByPercent(double percent, double number)
        {
            var newNumber = number * percent;
            return (int)newNumber;
        }

        public Bitmap ChangeOpacity(Image img, float opacityvalue)
        {
            Bitmap bmp = new Bitmap(img.Width, img.Height); // Determining Width and Height of Source Image
            Graphics graphics = System.Drawing.Graphics.FromImage(bmp);
            ColorMatrix colormatrix = new ColorMatrix();
            colormatrix.Matrix33 = opacityvalue;
            ImageAttributes imgAttribute = new ImageAttributes();
            imgAttribute.SetColorMatrix(colormatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            graphics.DrawImage(img, new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, imgAttribute);
            graphics.Dispose();   // Releasing all resource used by graphics
            return bmp;
        }

        private void SaveImage()
        {
            string CorrectURL = "";
            List<string> collectedURLS = new List<string>();

            WebClient w = new WebClient();
            string s = w.DownloadString(poe_lab_diff_url);

            // Collect all urls
            foreach (LinkItem i in LinkFinder.Find_SRC(s))
            {
                if (i.Href != null)
                {
                    Console.WriteLine(i.Href);
                    collectedURLS.Add(i.Href);
                }
            }

            // find wanted url
            foreach (string link in collectedURLS)
            {
                if (link.Contains("poelab.com/wp-content/uploads") && link.Contains(poe_lab_diff) && link.Contains("."))
                    CorrectURL = link;
            }

            var request = WebRequest.Create(CorrectURL);

            using (var response = request.GetResponse())
            using (var stream = response.GetResponseStream())
            {

                int x = 302,
                    y = 111,
                    width = 841,
                    height = 270;

                Bitmap source = (Bitmap)Image.FromStream(stream);
                Bitmap CroppedImage = source.Clone(new System.Drawing.Rectangle(x, y, width, height), source.PixelFormat);
                Bitmap NewImage = ChangeOpacity(CroppedImage, float.Parse(Settings.Opacity.Value.ToString()) / 100);
                CurrentFileName = GetUniqueKey(25);
                NewImage.Save(PluginDirectory + $"\\images\\{CurrentFileName}.png", ImageFormat.Png);
            }
        }

        private void GetRightPage()
        {
            List<string> collectedURLS = new List<string>();

            WebClient w = new WebClient();
            string s = w.DownloadString(poe_lab_url);

            // Collect all urls
            foreach (LinkItem i in LinkFinder.Find_HRef(s))
            {
                if (i.Href != null && i.Text.Contains(" lab notes"))
                {
                    //Console.WriteLine(i.Text);
                    collectedURLS.Add(i.Href);
                }
            }

            // find wanted url
            foreach (string link in collectedURLS)
            {
                if (!link.Contains("#comment"))
                {
                    if (link.Contains($"http://www.poelab.com/{poe_lab_diff}-lab-notes"))
                    {
                        poe_lab_diff_url = link;
                        break;
                    }
                }
            }
        }

        public static string GetUniqueKey(int maxSize)
        {
            char[] chars = new char[62];
            chars =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
            byte[] data = new byte[1];
            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetNonZeroBytes(data);
                data = new byte[maxSize];
                crypto.GetNonZeroBytes(data);
            }
            StringBuilder result = new StringBuilder(maxSize);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length)]);
            }
            return result.ToString();
        }
    }
    public struct LinkItem
    {
        public string Href;
        public string Text;

        public override string ToString()
        {
            return Href + "\n\t" + Text;
        }
    }

    static class LinkFinder
    {
        public static List<LinkItem> Find_SRC(string file)
        {
            List<LinkItem> list = new List<LinkItem>();

            // 1.
            // Find all matches in file.
            MatchCollection m1 = Regex.Matches(file, @"(<a.*?>.*?</a>)",
                RegexOptions.Singleline);

            // 2.
            // Loop over each match.
            foreach (Match m in m1)
            {
                string value = m.Groups[1].Value;
                LinkItem i = new LinkItem();

                // 3.
                // Get href attribute.
                Match m2 = Regex.Match(value, @"src=\""(.*?)\""",
                    RegexOptions.Singleline);
                if (m2.Success)
                {
                    i.Href = m2.Groups[1].Value;
                }

                // 4.
                // Remove inner tags from text.
                string t = Regex.Replace(value, @"\s*<.*?>\s*", "",
                    RegexOptions.Singleline);
                i.Text = t;

                list.Add(i);
            }
            return list;
        }
        public static List<LinkItem> Find_HRef(string file)
        {
            List<LinkItem> list = new List<LinkItem>();

            // 1.
            // Find all matches in file.
            MatchCollection m1 = Regex.Matches(file, @"(<a.*?>.*?</a>)",
                RegexOptions.Singleline);

            // 2.
            // Loop over each match.
            foreach (Match m in m1)
            {
                string value = m.Groups[1].Value;
                LinkItem i = new LinkItem();

                // 3.
                // Get href attribute.
                Match m2 = Regex.Match(value, @"href=\""(.*?)\""",
                    RegexOptions.Singleline);
                if (m2.Success)
                {
                    i.Href = m2.Groups[1].Value;
                }

                // 4.
                // Remove inner tags from text.
                string t = Regex.Replace(value, @"\s*<.*?>\s*", "",
                    RegexOptions.Singleline);
                i.Text = t;

                list.Add(i);
            }
            return list;
        }
    }

    public static class Keyboard
    {
        [DllImport("user32.dll")]
        private static extern uint keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        private const int KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const int KEYEVENTF_KEYUP = 0x0002;

        private const int ACTION_DELAY = 1;



        public static void KeyDown(Keys key)
        {
            keybd_event((byte)key, 0, KEYEVENTF_EXTENDEDKEY | 0, 0);
        }

        public static void KeyUp(Keys key)
        {
            keybd_event((byte)key, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0); //0x7F
        }

        public static void KeyPress(Keys key)
        {
            KeyDown(key);
            Thread.Sleep(ACTION_DELAY);
            KeyUp(key);
        }

        [DllImport("USER32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        public static bool IsKeyDown(int nVirtKey)
        {
            return GetKeyState(nVirtKey) < 0;

        }
    }
}
