﻿using HarmonyLib;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SpaceEngineers.Game.Entities.Blocks;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game.Utils;
using VRage.Input;
using VRage.Plugins;
using VRage.Utils;
using VRageMath;

namespace ScrollableFOV
{
    public class ScrollableFOV : IPlugin
    {

        private static float modFOV = -1;
        private static bool ModHasControl = false;

        private static float desiredFOV = -1;
        private static float originalFOV = -1;

        private bool isRegistered = false;

        private bool toggledFOV = false;
        private int lastPress = 0;

        private float lerpSpeed = .15f;

        private static float inhibitor;

        public ScrollableFOV()
        {
            Harmony harm = new Harmony("ScrollableFOV");
            harm.Patch(AccessTools.DeclaredMethod(typeof(MyVRageInput), "GetMouseXForGamePlay"), null, new HarmonyMethod(typeof(ScrollableFOV), nameof(GetPositionDelta)));
            harm.Patch(AccessTools.DeclaredMethod(typeof(MyVRageInput), "GetMouseYForGamePlay"), null, new HarmonyMethod(typeof(ScrollableFOV), nameof(GetPositionDelta)));
            harm.Patch(AccessTools.DeclaredMethod(typeof(MyVRageInput), "GetMouseXForGamePlayF"), null, new HarmonyMethod(typeof(ScrollableFOV), nameof(GetPositionDeltaF)));
            harm.Patch(AccessTools.DeclaredMethod(typeof(MyVRageInput), "GetMouseYForGamePlayF"), null, new HarmonyMethod(typeof(ScrollableFOV), nameof(GetPositionDeltaF)));
        }

        private static void GetPositionDeltaF(ref float __result)
        {
            if (inhibitor != 0)
                __result = (__result * inhibitor);
        }

        private static void GetPositionDelta(ref int __result)
        {
            if (inhibitor != 0)
                __result = (int)(__result * inhibitor);
        }

        public void Update()
        {
            lastPress--;

            if (!isRegistered && MyAPIUtilities.Static != null)
            {
                MyLog.Default.WriteLineAndConsole("ScrollableFOV: Registering mod API");
                MyAPIUtilities.Static.RegisterMessageHandler(9523876529384576, DoAPIStuff);
                isRegistered = true;
            }

            if (MyAPIGateway.Session?.Camera != null)
            {
                float currFov = MyAPIGateway.Session.Camera.FovWithZoom;

                if (desiredFOV == -1)
                {
                    desiredFOV = currFov;
                    originalFOV = desiredFOV;
                    modFOV = desiredFOV;
                    return;
                }

                if (!ModHasControl)
                {
                    if (MyAPIGateway.Input.IsNewKeyPressed(MyKeys.CapsLock))
                    {
                        if (lastPress >= 0)
                        {
                            toggledFOV = !toggledFOV;
                        }
                        lastPress = 25;
                    }

                    if (MyAPIGateway.Input.IsKeyPress(MyKeys.Control))
                    {
                        float prev = lerpSpeed;
                        if (MyAPIGateway.Input.IsKeyPress(MyKeys.PageUp))
                        {
                            lerpSpeed *= 1.02f;
                        }
                        else if (MyAPIGateway.Input.IsKeyPress(MyKeys.PageDown))
                        {
                            lerpSpeed /= 1.02f;
                        }

                        if (lerpSpeed != prev)
                        {
                            lerpSpeed = (float)MathHelper.Clamp(lerpSpeed, .01, .50);
                            MyAPIGateway.Utilities.ShowNotification("Smoothing: " + ((int)(lerpSpeed * 100)), 16);
                        }
                    }

                    if (MyAPIGateway.Input.IsKeyPress(MyKeys.CapsLock) || toggledFOV)
                    {
                        float delta = MyAPIGateway.Input.DeltaMouseScrollWheelValue();
                        if (MyAPIGateway.Input.IsKeyPress(MyKeys.CapsLock) && delta != 0)
                        {
                            desiredFOV = MathHelper.Clamp(desiredFOV - MathHelper.ToRadians(delta / 100f), 0.018f, 2.5f);
                            MyAPIGateway.Utilities.ShowNotification("Fov: " + Math.Round(MathHelper.ToDegrees(currFov), 1), 20);
                        }
                        if (Math.Round(desiredFOV, 2) != Math.Round(currFov, 2))
                        {
                            SetToDesiredFov((float)MathHelper.Lerp(currFov, desiredFOV, lerpSpeed));
                        }
                    }
                    else if (Math.Round(originalFOV, 2) != Math.Round(currFov, 2))
                    {
                        SetToDesiredFov((float)MathHelper.Lerp(currFov, originalFOV, lerpSpeed));
                    }
                    else
                    {
                        inhibitor = 0;
                    }
                }
                else
                {
                    SetToDesiredFov(modFOV);
                }

            }
            else
            {
                desiredFOV = -1;
            }
        }

        private void SetToDesiredFov(float fov)
        {
            if ((MyAPIGateway.Session.CameraController is MyCharacter) || 
                (MyAPIGateway.Session.CameraController is MyCockpit) ||
                (MyAPIGateway.Session.CameraController is MySpectatorCameraController))
            {
                MySector.MainCamera.FieldOfView = fov;
                inhibitor = Math.Min(1, fov);
            }
        }

        public void Init(object gameInstance)
        {
            MySession.OnUnloading += () => { ModHasControl = false; };
        }

        private Dictionary<string, Delegate> ModApiMethods = new Dictionary<string, Delegate>()
        {
            ["SetFov"] = new Action<float>(SetFov),
            ["ResetFov"] = new Action(ResetFOV),
            ["SetModControl"] = new Action<bool>(SetModControl),
            ["DoesModHaveControl"] = new Func<bool>(DoesModHaveControl),
        };

        public void DoAPIStuff(object obj)
        {
            if (obj == null || !(obj is string))
                return;

            string[] call = (obj as string).Split(':');

            if (call != null && call.Length == 2 && call[0] == "RequestingAPI")
            {
                if (call[1] != "1")
                {
                    MyAPIGateway.Utilities.ShowMessage("AnimationAPI", $"Animation API outdated :: Expected '1' Got '{call[1]}'");
                }
                MyLog.Default.WriteLineAndConsole("ScrollableFOV: A mod is requesting the mod API!");
                MyAPIGateway.Utilities.SendModMessage(9523876529384574, ModApiMethods);
            }
        }

        public static void SetFov(float fov)
        {
            modFOV = fov;
        }

        public static void ResetFOV()
        {
            modFOV = originalFOV;
        }

        public static void SetModControl(bool value)
        {
            ModHasControl = value;
        }

        public static bool DoesModHaveControl()
        {
            return ModHasControl;
        }

        public void Dispose() { }
    }

    /// <summary>
    /// Mod API for Scrollable FOV
    /// Copy and paste this into your mod, call init
    /// and then use as you want.
    /// be sure to call Close when the mods shuts down
    /// </summary>
    public static class FOVAPI
    {
        private static Action<bool> _SetModControl;
        private static Func<bool> _DoesModHaveControl;
        private static Action _ResetFOV;
        private static Action<float> _SetFOV;

        /// <summary>
        /// Call on init or when you want to use the API
        /// </summary>
        public static void Init()
        {
            MyAPIGateway.Utilities.RegisterMessageHandler(9523876529384574, APIAssignment);
            MyAPIGateway.Utilities.SendModMessage(9523876529384576, "RequestingAPI:1");
        }

        /// <summary>
        /// Call on mod close
        /// </summary>
        public static void Close()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(9523876529384576, APIAssignment);
        }

        public static void SetModControl(bool hasControl) => _SetModControl.Invoke(hasControl);
        public static bool DoesModHaveControl() => _DoesModHaveControl.Invoke();
        public static void ResetFOV() => _ResetFOV.Invoke();
        public static void SetFOV(float fov) => _SetFOV.Invoke(fov);
        public static bool IsInstalled() => _SetFOV != null;


        private static void APIAssignment(object obj)
        {
            var dict = obj as IReadOnlyDictionary<string, Delegate>;

            if (dict == null)
                return;

            AssignDelegate(dict, "SetFov", ref _SetFOV);
            AssignDelegate(dict, "ResetFov", ref _ResetFOV);
            AssignDelegate(dict, "SetModControl", ref _SetModControl);
            AssignDelegate(dict, "DoesModHaveControl", ref _DoesModHaveControl);
        }

        private static void AssignDelegate<T>(IReadOnlyDictionary<string, Delegate> delegates, string name, ref T field) where T : class
        {
            if (delegates == null)
            {
                field = null;
                return;
            }

            Delegate del;
            if (!delegates.TryGetValue(name, out del))
                throw new Exception($"FOVAPI :: Couldn't find {name} delegate of type {typeof(T)}");

            field = del as T;

            if (field == null)
                throw new Exception($"FOVAPI :: Delegate {name} is not type {typeof(T)}, instead it's: {del.GetType()}");
        }
    }




}
