using System;
using System.Collections.Generic;
using System.Threading;
using PicoBridge;
using PicoBridge.Types;
using ViveSR.anipal.Lip;
using VRCFaceTracking;
using VRCFaceTracking.Params;

namespace PicoVRCFTModule;

public static class TrackingData
{
    private static float ape(float jawOpen, float mouthClose) =>
        (0.05f + jawOpen) * (0.05f + mouthClose) * (0.05f + mouthClose);

    private static float eyeOpenness(float blink, float squint) => (float) (Math.Pow(0.05 + blink, 6) + squint);

    public static void Update(ref EyeTrackingData data, PicoFaceTrackingDatagram external)
    {
        data.SupportsImage = false;

        data.Left.Look = new Vector2(external.LeftEyeYaw, external.LeftEyePitch);
        data.Right.Look = new Vector2(external.RightEyeYaw, external.RightEyePitch);
        data.Left.Openness = 1 - eyeOpenness(external[PicoBlendShapeWeight.EyeBlinkLeft],
            external[PicoBlendShapeWeight.EyeSquintLeft]);
        data.Right.Openness = 1 - eyeOpenness(external[PicoBlendShapeWeight.EyeBlinkRight],
            external[PicoBlendShapeWeight.EyeSquintRight]);
        data.Left.Widen = external[PicoBlendShapeWeight.EyeWideLeft];
        data.Right.Widen = external[PicoBlendShapeWeight.EyeWideRight];
        data.Combined.Look = new Vector2(external.CombinedYaw, external.CombinedPitch);
    }

    public static void Update(ref LipTrackingData data, PicoFaceTrackingDatagram external)
    {
        var lipShapes = new Dictionary<LipShape_v2, float>
        {
            {LipShape_v2.JawLeft, external[PicoBlendShapeWeight.JawLeft]},
            {LipShape_v2.JawRight, external[PicoBlendShapeWeight.JawRight]},
            {LipShape_v2.JawOpen, external[PicoBlendShapeWeight.JawOpen]},
            {LipShape_v2.JawForward, external[PicoBlendShapeWeight.JawForward]},
            {
                LipShape_v2.MouthApeShape,
                ape(external[PicoBlendShapeWeight.JawOpen], external[PicoBlendShapeWeight.MouthClose])
            },
            {LipShape_v2.MouthUpperLeft, external[PicoBlendShapeWeight.MouthUpperUpLeft]},
            {LipShape_v2.MouthUpperRight, external[PicoBlendShapeWeight.MouthUpperUpRight]},
            {LipShape_v2.MouthLowerLeft, external[PicoBlendShapeWeight.MouthLowerDownLeft]},
            {LipShape_v2.MouthLowerRight, external[PicoBlendShapeWeight.MouthLowerDownRight]},
            {LipShape_v2.MouthUpperOverturn, external[PicoBlendShapeWeight.MouthShrugUpper]},
            {LipShape_v2.MouthLowerOverturn, external[PicoBlendShapeWeight.MouthShrugLower]},
            {
                LipShape_v2.MouthPout,
                (external[PicoBlendShapeWeight.MouthFunnel] + external[PicoBlendShapeWeight.MouthPucker]) / 2
            },
            {LipShape_v2.MouthSmileLeft, external[PicoBlendShapeWeight.MouthSmileLeft]},
            {LipShape_v2.MouthSmileRight, external[PicoBlendShapeWeight.MouthSmileRight]},
            {LipShape_v2.MouthSadLeft, external[PicoBlendShapeWeight.MouthFrownLeft]},
            {LipShape_v2.MouthSadRight, external[PicoBlendShapeWeight.MouthFrownRight]},
            {LipShape_v2.CheekPuffLeft, external[PicoBlendShapeWeight.CheekPuff]},
            {LipShape_v2.CheekPuffRight, external[PicoBlendShapeWeight.CheekPuff]},
            {LipShape_v2.CheekSuck, 0.0f},
            {LipShape_v2.MouthUpperUpLeft, external[PicoBlendShapeWeight.MouthUpperUpLeft]},
            {LipShape_v2.MouthUpperUpRight, external[PicoBlendShapeWeight.MouthUpperUpRight]},
            {LipShape_v2.MouthLowerDownLeft, external[PicoBlendShapeWeight.MouthLowerDownLeft]},
            {LipShape_v2.MouthLowerDownRight, external[PicoBlendShapeWeight.MouthLowerDownRight]},
            {LipShape_v2.MouthUpperInside, external[PicoBlendShapeWeight.MouthRollUpper]},
            {LipShape_v2.MouthLowerInside, external[PicoBlendShapeWeight.MouthRollLower]},
            {LipShape_v2.MouthLowerOverlay, 0.0f},
            {LipShape_v2.TongueLongStep1, external[PicoBlendShapeWeight.TongueOut]},
            {LipShape_v2.TongueLeft, 0.0f},
            {LipShape_v2.TongueRight, 0.0f},
            {LipShape_v2.TongueUp, 0.0f},
            {LipShape_v2.TongueDown, 0.0f},
            {LipShape_v2.TongueRoll, 0.0f},
            {LipShape_v2.TongueLongStep2, external[PicoBlendShapeWeight.TongueOut]},
            {LipShape_v2.TongueUpRightMorph, 0.0f},
            {LipShape_v2.TongueUpLeftMorph, 0.0f},
            {LipShape_v2.TongueDownLeftMorph, 0.0f},
            {LipShape_v2.TongueDownRightMorph, 0.0f}
        };

        foreach (var keyValuePair in lipShapes)
        {
            data.LatestShapes[(int) keyValuePair.Key] = keyValuePair.Value;
        }
    }

    public static void Update(PicoFaceTrackingDatagram datagram)
    {
        Update(ref UnifiedTrackingData.LatestEyeData, datagram);
        Update(ref UnifiedTrackingData.LatestLipData, datagram);
    }
}

public class PicoVrcftModule : ExtTrackingModule
{
    private readonly PicoBridgeServer server = new();
    private PicoFaceTrackingDatagram datagram = new();
    private long datagramCounter;
    private bool isDeviceConnected;
    private DateTime lastLogTime = DateTime.Now;
    private long lastUpdateTimestamp;
    private CancellationTokenSource token = new();
    public override (bool SupportsEye, bool SupportsLip) Supported => (true, true);

    public override (bool eyeSuccess, bool lipSuccess) Initialize(bool eye, bool lip)
    {
        Logger.Msg("Starting PICO server");
        UnifiedTrackingData.LatestEyeData.SupportsImage = false;
        UnifiedTrackingData.LatestLipData.SupportsImage = false;
        datagramCounter = 0;

        try
        {
            server.DatagramChange += OnDatagram;
            server.ConnectivityChange += (_, state) =>
            {
                isDeviceConnected = state;
                Logger.Msg(state ? "PICO connected" : "PICO disconnected");
            };
            server.Start();
            Logger.Msg("PICO server started, waiting for connection");
        }
        catch (Exception)
        {
            Logger.Error("Port already in use. Run PICOBridgeHelper.exe first!");
            return (false, false);
        }

        return (true, true);
    }

    public void Update()
    {
        if (!isDeviceConnected)
        {
            return;
        }

        if (datagram.Timestamp == lastUpdateTimestamp)
        {
            return;
        }

        TrackingData.Update(datagram);
        lastUpdateTimestamp = datagram.Timestamp;
    }

    public override Action GetUpdateThreadFunc()
    {
        token = new CancellationTokenSource();
        return () =>
        {
            var now = DateTime.Now;
            while (!token.IsCancellationRequested)
            {
                Update();
                Thread.Sleep(10);
                if (!isDeviceConnected || now - lastLogTime < TimeSpan.FromSeconds(5)) continue;

                Logger.Msg($"{datagramCounter / 5} datagram/s");
                datagramCounter = 0;
                lastLogTime = DateTime.Now;
            }
        };
    }

    public override void Teardown()
    {
        server.Stop();
        server.Join();
    }

    private void OnDatagram(object sender, PicoFaceTrackingDatagram data)
    {
        datagram = data;
        ++datagramCounter;
    }
}
