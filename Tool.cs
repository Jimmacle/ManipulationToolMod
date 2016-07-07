using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Jimmacle.Manipulator
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ManipulationComponent : MySessionComponentBase
    {
        private const ushort HANDLER_ID = 14268;

        private bool init = false;
        private bool grabbed = false;
        private Grabber grabber;
        private List<Grabber> otherGrabbers = new List<Grabber>();
        private IMyInput input;

        private NotificationMenu menu;

        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Session != null)
            {
                if (!init)
                {
                    MyAPIGateway.Multiplayer.RegisterMessageHandler(HANDLER_ID, MessageHandler);
                    input = MyAPIGateway.Input;
                    Settings.Load();
                    menu = new NotificationMenu();
                    init = true;
                }

                if (input.IsNewMiddleMousePressed())
                {
                    menu.Toggle();
                }

                if (!menu.IsOpen)
                {
                    if (input.IsNewPrimaryButtonPressed())
                    {
                        LinkHandler.TryMake();
                        /*
                        if (Settings.Static.ToggleGrab && grabbed)
                        {
                            Release();
                        }
                        else if (!grabbed)
                        {
                            Grab();
                        }*/
                    }
                    else if (input.IsNewPrimaryButtonReleased())
                    {
                        if (!Settings.Static.ToggleGrab)
                        {
                            Release();
                        }
                    }
                }

                UpdateRemoteGrabbers();
                menu.Update();

                if (grabbed)
                {
                    grabber.MoveGrabbed();
                }

                LinkHandler.Update();
            }
        }

        private void Grab()
        {
            if (MyAPIGateway.Session.ControlledObject != null && MyGuiScreenTerminal.GetCurrentScreen() == MyTerminalPageEnum.None && MyGuiScreenGamePlay.ActiveGameplayScreen == null)
            {
                if (grabber == null)
                {
                    grabber = new Grabber(MyAPIGateway.Session.Player);
                }

                if (grabber.TryGrabNew(5))
                {
                    List<byte> packet = new List<byte>();
                    packet.AddRange(BitConverter.GetBytes(true));
                    packet.AddRange(BitConverter.GetBytes(MyAPIGateway.Session.Player.IdentityId));

                    MyAPIGateway.Multiplayer.SendMessageToOthers(HANDLER_ID, packet.ToArray());

                    grabbed = true;
                }
            }
        }

        private void Release()
        {
            if (grabbed)
            {
                if (MyAPIGateway.Session.ControlledObject != null)
                {
                    List<byte> packet = new List<byte>();
                    packet.AddRange(BitConverter.GetBytes(false));
                    packet.AddRange(BitConverter.GetBytes(MyAPIGateway.Session.Player.IdentityId));

                    MyAPIGateway.Multiplayer.SendMessageToOthers(HANDLER_ID, packet.ToArray());

                    grabber.ReleaseGrabbed();
                    grabbed = false;
                }
            }
        }

        private void UpdateRemoteGrabbers()
        {
            foreach (var grab in otherGrabbers)
            {
                grab.MoveGrabbed();
            }
        }

        public void MessageHandler(byte[] message)
        {
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Multiplayer.Players.GetPlayers(players);

            bool grabbed = BitConverter.ToBoolean(message, 0);
            long identity = BitConverter.ToInt64(message, 1);

            if (identity == MyAPIGateway.Session.Player.IdentityId)
            {
                return;
            }

            if (grabbed)
            {
                var player = players.Find(p => p.IdentityId == identity);
                if (player != null)
                {
                    var grabber = new Grabber(player);
                    grabber.TryGrabNew(5);

                    otherGrabbers.Add(grabber);
                }
            }
            else
            {
                var grabber = otherGrabbers.Find(g => g.Player.IdentityId == identity);
                if (grabber != null)
                {
                    grabber.ReleaseGrabbed();
                    otherGrabbers.Remove(grabber);
                }
            }
        }

        public override void SaveData()
        {
            Settings.Save();
        }
    }
}
