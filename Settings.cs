using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;

namespace Jimmacle.Manipulator
{
    public class Settings
    {
        private static Settings s = new Settings();
        public static Settings Static { get { return s; } }

        private float opacity = 0.2f;
        private float forceMult = 1f;

        public bool ToggleGrab { get; set; }
        public float Opacity
        {
            get
            {
                return opacity;
            }
            set
            {
                opacity = Math.Max(0f, Math.Min(value, 1f));
            }
        }
        public float ForceMult
        {
            get
            {
                return forceMult;
            }
            set
            {
                forceMult = Math.Max(0.9f, Math.Min(value, 1.2f));
            }
        }

        public static void Load()
        {
            if (MyAPIGateway.Utilities.FileExistsInLocalStorage("Settings.cfg", typeof(Settings)))
            {
                try
                {
                    using (var file = MyAPIGateway.Utilities.ReadFileInLocalStorage("Settings.cfg", typeof(Settings)))
                    {
                        s = MyAPIGateway.Utilities.SerializeFromXML<Settings>(file.ReadToEnd());
                    }
                }
                catch
                {
                    if (Extensions.HasGUI())
                    {
                        MyAPIGateway.Utilities.ShowNotification("Failed to load Manipulator settings.", 4000, VRage.Game.MyFontEnum.Red);
                    }
                }
            }
        }

        public static void Save()
        {
            try
            {
                using (var file = MyAPIGateway.Utilities.WriteFileInLocalStorage("Settings.cfg", typeof(Settings)))
                {
                    var serialized = MyAPIGateway.Utilities.SerializeToXML(s);
                    file.Write(serialized);
                }
            }
            catch
            {
                if (Extensions.HasGUI())
                {
                    MyAPIGateway.Utilities.ShowNotification("Failed to save Manipulator settings.", 4000, VRage.Game.MyFontEnum.Red);
                }
            }
        }
    }
}
