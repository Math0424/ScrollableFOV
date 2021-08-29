using Sandbox.Engine.Platform.VideoMode;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using VRage.Input;
using VRage.Plugins;
using VRageMath;

namespace ScrollableFOV
{
    public class ScrollableFOV : IPlugin
    {
        private float desiredFOV = -1;

        private float originalFOV = -1;
        private float originalSensitivity = -1;

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
                else if(g.FieldOfView != originalFOV)
                {
                    SetToDesiredFov(originalFOV);
                    MyInput.Static.SetMouseSensitivity(originalSensitivity);
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
        }

        private void RegisterHandler()
        {
            MyAPIGateway.Utilities.RegisterMessageHandler(9523876529384576, SetFov);
        }

        private void SetFov(object data)
        {
            float fov = (float)data;
            if (MyAPIGateway.Session?.Camera != null)
            {
                var g = MyVideoSettingsManager.CurrentGraphicsSettings;
                g.FieldOfView = fov;
            }
        }

        public void Dispose()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(9523876529384576, SetFov);
        }

    }
}
