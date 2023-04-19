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
    private const float EyeCloseThreshold = 0.25f;
    private const float EyeWidenThreshold = 0.98f;

    private static int logCounter = 0;

    private static float Ape(float jawOpen, float mouthClose) =>
        (0.05f + jawOpen) * (0.05f + mouthClose) * (0.05f + mouthClose);

    private static float EyeOpenness(float blink, float squint) => 1.0f - Math.Max(0, Math.Min(1, blink)) +
                                                                   (float) (blink * (2f * squint) /
                                                                            Math.Pow(2f, 2f * squint));

    private static float Trigger(float input, float lowerBound, float trigger, float upperBound)
    {
        if (input < trigger)
        {
            if (input < lowerBound)
            {
                return 0.0f;
            }

            return input / (trigger - lowerBound) / 2;
        }
        else
        {
            if (input > upperBound)
            {
                return 1.0f;
            }

            return 0.5f + (input - trigger) / (upperBound - trigger) / 2;
        }
    }

    private static float Clamp(float input, float lowerBound, float upperBound) =>
        Math.Min(Math.Max(lowerBound, input), upperBound);

    private static void UpdateEye(ref Eye eye, float openness)
    {
        switch (openness)
        {
            case >= EyeWidenThreshold:
                eye.Openness = 1.0f;
                eye.Squeeze = 0;
                eye.Widen = (openness - EyeWidenThreshold) / (1 - EyeWidenThreshold);
                break;
            case <= EyeCloseThreshold:
                eye.Openness = 0.0f;
                eye.Squeeze = (openness / -EyeCloseThreshold) + 1;
                eye.Widen = 0;
                break;
            default:
                eye.Openness = openness;
                eye.Squeeze = 0;
                eye.Widen = 0;
                break;
        }
    }

    public static void Update(ref EyeTrackingData data, PicoFaceTrackingDatagram external)
    {
        data.SupportsImage = false;

        // Note: I tried to fix it into calculated properties but the sign is always incorrect for some reason
        var leftYaw = Clamp(external[PicoBlendShapeWeight.EyeLookInLeft] -
                            external[PicoBlendShapeWeight.EyeLookOutLeft], -0.8f, 0.8f);
        var leftPitch = external[PicoBlendShapeWeight.EyeLookUpLeft] - external[PicoBlendShapeWeight.EyeLookDownLeft];
        var rightYaw =
            Clamp(external[PicoBlendShapeWeight.EyeLookOutRight] - external[PicoBlendShapeWeight.EyeLookInRight], -0.8f,
                0.8f);
        var rightPitch = external[PicoBlendShapeWeight.EyeLookUpRight] -
                         external[PicoBlendShapeWeight.EyeLookDownRight];
        var averageYaw = Clamp((leftYaw + rightYaw) / 2, -0.8f, 0.8f);
        var averagePitch = Clamp((leftPitch + rightPitch) / 2, -0.8f, 0.8f);

        var leftOpenness = EyeOpenness(external[PicoBlendShapeWeight.EyeBlinkLeft],
            external[PicoBlendShapeWeight.EyeSquintLeft]);
        var rightOpenness = EyeOpenness(external[PicoBlendShapeWeight.EyeBlinkRight],
            external[PicoBlendShapeWeight.EyeSquintRight]);
        var averageOpenness = Clamp((leftOpenness + rightOpenness) / 2, 0, 1.0f);

        data.Left.Look = new Vector2(leftYaw, leftPitch);
        data.Right.Look = new Vector2(rightYaw, rightPitch);
        data.Combined.Look = new Vector2(averageYaw, averagePitch);

        UpdateEye(ref data.Left, leftOpenness);
        UpdateEye(ref data.Right, rightOpenness);
        UpdateEye(ref data.Combined, averageOpenness);

        if (++logCounter < 10) return;

        Logger.Msg($"Lo={leftOpenness},Ro={rightOpenness},Vo={averageOpenness}");
        logCounter = 0;
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
                Ape(external[PicoBlendShapeWeight.JawOpen], external[PicoBlendShapeWeight.MouthClose])
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
