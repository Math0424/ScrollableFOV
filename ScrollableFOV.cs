using Sandbox.Engine.Platform.VideoMode;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
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

        private float originalSensitivity = -1;
        private bool isRegistered = false;

        private bool toggledFOV = false;
        private int lastPress = 0;

        private float lerpSpeed = .15f;

        public void Update()
        {
            lastPress--;

            if (MyAPIUtilities.Static != null && !isRegistered)
            {
                MyLog.Default.WriteLineAndConsole("ScrollableFOV: Registering mod API");
                MyAPIUtilities.Static.RegisterMessageHandler(9523876529384576, DoAPIStuff);
                isRegistered = true;
            }

            if (MyAPIGateway.Session?.Camera != null)
            {
                var g = MyVideoSettingsManager.CurrentGraphicsSettings;
                if (desiredFOV == -1)
                {
                    if (g.FieldOfView > 1)
                    {
                        desiredFOV = g.FieldOfView;
                        originalFOV = desiredFOV;
                        originalSensitivity = MyInput.Static.GetMouseSensitivity();
                        modFOV = desiredFOV;
                    }
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
                        else if(MyAPIGateway.Input.IsKeyPress(MyKeys.PageDown))
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
                            MyAPIGateway.Utilities.ShowNotification("Fov: " + Math.Round(MathHelper.ToDegrees(g.FieldOfView), 1), 20);
                        }
                        if (Math.Round(desiredFOV, 2) != Math.Round(g.FieldOfView, 2))
                        {
                            SetToDesiredFov((float)MathHelper.Lerp(g.FieldOfView, desiredFOV, lerpSpeed));
                        }
                    }
                    else if (Math.Round(originalFOV, 2) != Math.Round(g.FieldOfView, 2))
                    {
                        SetToDesiredFov((float)MathHelper.Lerp(g.FieldOfView, originalFOV, lerpSpeed));
                    }
                    else
                    {
                        MyInput.Static.SetMouseSensitivity(originalSensitivity);
                    }
                } 
                else
                {
                    SetToDesiredFov(modFOV);
                }

            }
        }

        private void SetToDesiredFov(float fov)
        {
            var g = MyVideoSettingsManager.CurrentGraphicsSettings;
            g.FieldOfView = fov;
            MyInput.Static.SetMouseSensitivity(Math.Min(1, g.FieldOfView));
            MyVideoSettingsManager.Apply(g);
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

        public void Dispose(){}
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
