using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Labyrinth_Layout
{
    public class PluginSettings : SettingsBase
    {
        public PluginSettings()
        {
            Opacity = new RangeNode<int>(85, 0, 100);
            Size = new RangeNode<int>(65, 0, 150);
            X = new RangeNode<float>(65, 0, BasePlugin.API.GameController.Window.GetWindowRectangle().BottomRight.X);
            Y = new RangeNode<float>(65, 0, BasePlugin.API.GameController.Window.GetWindowRectangle().BottomRight.Y);
            Difficulty = new ListNode();
            LoadOnStart = true;
            OnlyLabyrinth = true;
            HotKey = Keys.Insert;
        }


        [Menu("(Re)Load Hotkey", 400)]
        public HotkeyNode HotKey { get; set; }

        [Menu("Difficulty", 1)]
        public ListNode Difficulty { get; set; }


        [Menu("Location", 300)]
        public EmptyNode Location { get; set; }
        [Menu("X", 301, 300)]
        public RangeNode<float> X { get; set; }
        [Menu("Y", 302, 300)]
        public RangeNode<float> Y { get; set; }

        [Menu("Opacity", 100)]
        public RangeNode<int> Opacity { get; set; }

        [Menu("Size", 200)]
        public RangeNode<int> Size { get; set; }

        [Menu("", 800)]
        public EmptyNode ccc { get; set; }

        [Menu("Automatic Changing", 700)]
        public ToggleNode OnlyLabyrinth { get; set; }

        [Menu("Start On PoeHUD Load", 500)]
        public ToggleNode LoadOnStart { get; set; }

    }
}