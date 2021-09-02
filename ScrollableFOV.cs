using Sandbox.Engine.Platform.VideoMode;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Input;
using VRage.Plugins;
using VRageMath;

namespace ScrollableFOV
{
    public class ScrollableFOV : IPlugin
    {
        private static float desiredFOV = -1;

        private static float originalFOV = -1;
        private static float originalSensitivity = -1;

        private static bool ModHasControl = false;

        public void Update()
        {
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
                    }
                    return;
                }

                if (!ModHasControl)
                {
                    if (MyAPIGateway.Input.IsKeyPress(MyKeys.CapsLock))
                    {
                        float delta = MyAPIGateway.Input.DeltaMouseScrollWheelValue();
                        if (delta != 0)
                        {
                            desiredFOV = MathHelper.Clamp(desiredFOV - MathHelper.ToRadians(delta / 100f), 0.018f, 2.5f);

                            if (Math.Round(desiredFOV, 2) != Math.Round(g.FieldOfView, 2))
                            {
                                SetToDesiredFov((float)MathHelper.Lerp(g.FieldOfView, desiredFOV, .15));
                            }

                            MyAPIGateway.Utilities.ShowNotification("Fov: " + Math.Round(MathHelper.ToDegrees(g.FieldOfView), 1), 20);
                        }
                        else
                        {
                            SetToDesiredFov(desiredFOV);
                        }
                    }
                    else if (g.FieldOfView != originalFOV)
                    {
                        SetToDesiredFov(originalFOV);
                        MyInput.Static.SetMouseSensitivity(originalSensitivity);
                    }
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
            MySession.AfterLoading += RegisterHandler;
            MySession.OnUnloading += UnRegisterHandler;
        }

        private Dictionary<string, Delegate> ModApiMethods = new Dictionary<string, Delegate>()
        {
            ["SetFov"] = new Action<float>(SetFov),
            ["ResetFov"] = new Action(ResetFOV),
            ["SetModControl"] = new Action<bool>(SetModControl),
            ["DoesModHaveControl"] = new Func<bool>(DoesModHaveControl),
        };

        private void RegisterHandler()
        {
            MyAPIGateway.Utilities.RegisterMessageHandler(9523876529384576, DoAPIStuff);
        }
        private void UnRegisterHandler()
        {
            MyAPIGateway.Utilities.RegisterMessageHandler(9523876529384576, DoAPIStuff);
            ModHasControl = false;
        }

        public void DoAPIStuff(object obj)
        {
            string[] call = (obj as string).Split(':');

            if (call != null && call.Length == 2 && call[0] == "RequestingAPI")
            {
                if (call[1] != "1")
                {
                    MyAPIGateway.Utilities.ShowMessage("AnimationAPI", $"Animation API outdated :: Expected '1' Got '{call[1]}'");
                }
                MyAPIGateway.Utilities.SendModMessage(9523876529384576, ModApiMethods);
            }
        }

        public static void SetFov(float fov)
        {
            if (MyAPIGateway.Session?.Camera != null)
            {
                var g = MyVideoSettingsManager.CurrentGraphicsSettings;
                g.FieldOfView = fov;
            }
        }

        public static void ResetFOV()
        {
            if (MyAPIGateway.Session?.Camera != null)
            {
                var g = MyVideoSettingsManager.CurrentGraphicsSettings;
                g.FieldOfView = originalFOV;
                MyInput.Static.SetMouseSensitivity(originalSensitivity);
            }
        }

        public static void SetModControl(bool value)
        {
            ModHasControl = value;
        }

        public static bool DoesModHaveControl()
        {
            return ModHasControl;
        }



        public void Dispose()
        {
            UnRegisterHandler();
        }

    }


    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class FOVAPI : MySessionComponentBase
    {
        public Action<bool> _SetModControl;
        public Func<bool> _DoesModHaveControl;
        public Action _ResetFOV;
        public Action<float> _SetFOV;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            MyAPIGateway.Utilities.RegisterMessageHandler(9523876529384576, APIAssignment);
            MyAPIGateway.Utilities.SendModMessage(9523876529384576, "RequestingAPI:1");
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(9523876529384576, APIAssignment);
        }

        private void APIAssignment(object obj)
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
