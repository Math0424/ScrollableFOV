using Sandbox.Engine.Platform.VideoMode;
using Sandbox.ModAPI;
using System;
using VRage.Input;
using VRage.Plugins;
using VRageMath;

namespace ScrollableFOV
{
    public class ScrollableFOV : IPlugin
    {
        float desiredFOV = -1;

        public void Update()
        {
            if (MyAPIGateway.Session?.Camera != null)
            {
                var g = MyVideoSettingsManager.CurrentGraphicsSettings;
                if (desiredFOV == -1)
                {
                    if (g.FieldOfView > 1)
                        desiredFOV = g.FieldOfView;
                    return;
                }

                if (MyAPIGateway.Input.IsKeyPress(MyKeys.CapsLock))
                {
                    float delta = MyAPIGateway.Input.DeltaMouseScrollWheelValue();
                    if (delta != 0)
                    {
                        desiredFOV = MathHelper.Clamp(desiredFOV - MathHelper.ToRadians(delta / 100f), 0.018f, 2.5f);
                        MyAPIGateway.Utilities.ShowNotification("Fov: " + Math.Round(MathHelper.ToDegrees(g.FieldOfView), 1), 20);
                    }
                }

                if (Math.Round(desiredFOV, 2) != Math.Round(g.FieldOfView, 2))
                {
                    g.FieldOfView = (float)MathHelper.Lerp(g.FieldOfView, desiredFOV, .15);
                    MyInput.Static.SetMouseSensitivity(Math.Min(1, g.FieldOfView));
                    MyVideoSettingsManager.Apply(g);
                }
            }
        }
        public void Init(object gameInstance) {}
        public void Dispose() {}
    }
}
