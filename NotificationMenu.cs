using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Jimmacle.Manipulator
{
    public class NotificationMenu
    {
        public List<Item> Items = new List<Item>();
        private int index = 0;
        private Item selected;

        public bool IsOpen { get; private set; }

        public NotificationMenu()
        {
            Items.Add(new Item(name: "Manipulation Tool Options"));
            Items.Add(new Item(name: "MMB: Close, LMB: Select, Scroll: Change Setting"));

            var toggleItem = new Item()
            {
                DisplayFunc = x => $"Selection Mode: {(Settings.Instance.ToggleGrab ? "Toggle" : "Hold")}",
                ScrollUpAction = x => Settings.Instance.ToggleGrab = !Settings.Instance.ToggleGrab,
                ScrollDownAction = x => Settings.Instance.ToggleGrab = !Settings.Instance.ToggleGrab
            };

            var drawItem = new Item()
            {
                DisplayFunc = x => $"Indicator Opacity (0-1): {Settings.Instance.Opacity:0.00}",
                ScrollUpAction = x => Settings.Instance.Opacity += 0.02f,
                ScrollDownAction = x => Settings.Instance.Opacity -= 0.02f
            };

            var forceItem = new Item()
            {
                DisplayFunc = x => $"Force Multiplier (0.9-1.2): {Settings.Instance.ForceMult:0.00}",
                ScrollUpAction = x => Settings.Instance.ForceMult += 0.05f,
                ScrollDownAction = x => Settings.Instance.ForceMult -= 0.05f
            };

            Items.Add(toggleItem);
            Items.Add(drawItem);
            Items.Add(forceItem);
        }

        public void Open()
        {
            index = 0;
            IsOpen = true;
            selected = null;
            foreach (var item in Items)
            {
                item.ItemText.Show();
            }
        }

        public void Close()
        {
            IsOpen = false;
            selected = null;
            foreach (var item in Items)
            {
                item.ItemText.Hide();
            }
            index = 0;
        }

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        public void Update()
        {
            if (IsOpen)
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    var item = Items[i];

                    item.UpdateName();

                    if (item == selected)
                    {
                        item.ItemText.Font = VRage.Game.MyFontEnum.Green;
                    }
                    else if (i == index)
                    {
                        item.ItemText.Font = VRage.Game.MyFontEnum.DarkBlue;
                    }
                    else
                    {
                        item.ItemText.Font = VRage.Game.MyFontEnum.White;
                    }

                    item.ItemText.ResetAliveTime();
                }

                if (MyAPIGateway.Input.IsNewLeftMousePressed())
                {
                    selected = selected == null ? Items[index] : null;
                }

                var wheelSign = Math.Sign(MyAPIGateway.Input.DeltaMouseScrollWheelValue());

                if (selected != null)
                {
                    if (wheelSign > 0)
                    {
                        selected.ScrollUpAction?.Invoke(selected);
                    }
                    else if (wheelSign < 0)
                    {
                        selected.ScrollDownAction?.Invoke(selected);
                    }
                }
                else
                {
                    index -= wheelSign;
                }

                index = Math.Min(index, Items.Count - 1);
                index = Math.Max(index, 0);
            }
        }

        public class Item
        {
            public Action<Item> ScrollUpAction;
            public Action<Item> ScrollDownAction;
            public Func<Item, string> DisplayFunc;
            public IMyHudNotification ItemText;

            public void UpdateName()
            {
                if (DisplayFunc != null)
                    ItemText.Text = DisplayFunc.Invoke(this);
            }

            public Item(Func<Item, string> displayFunc = null, Action<Item> leftAction = null, Action<Item> rightAction = null, string name = "")
            {
                DisplayFunc = displayFunc;
                ScrollUpAction = leftAction;
                ScrollDownAction = rightAction;
                ItemText = MyAPIGateway.Utilities.CreateNotification(name);
            }
        }
    }
}
