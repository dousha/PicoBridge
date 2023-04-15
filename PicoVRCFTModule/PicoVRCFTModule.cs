using PicoBridge;
using PicoBridge.Types;
using VRCFaceTracking;
using VRCFaceTracking.Params;

namespace PicoVRCFTModule;

// This class contains the overrides for any VRCFT Tracking Data struct functions
public static class TrackingData
{
    // This function parses the external module's full-data data into UnifiedExpressions' eye structure.
    public static void UpdateEye(ref UnifiedEyeData data, PicoFaceTrackingDatagram external)
    {
    }

    // This function parses the external module's full-data data into the UnifiedExpressions' Shapes
    public static void UpdateExpressions(ref UnifiedTrackingData data, PicoFaceTrackingDatagram external)
    {
        data.Shapes[(int) UnifiedExpressions.JawOpen].Weight = external.jaw_open;
        data.Shapes[(int) UnifiedExpressions.TongueOut].Weight = external.tongue_out;
    }
}

public class PicoVrcftModule : ExtTrackingModule
{
    public override (bool eyeSuccess, bool lipSuccess) Initialize(bool eye, bool lip)
    {
        try
        {
            server.DatagramChange += OnDatagram;
            server.Start();
        }
        catch (Exception)
        {
            return (false, false);
        }

        return (true, true);
    }

    public override Action GetUpdateThreadFunc()
    {
        return () =>
        {
            while (!token.IsCancellationRequested)
            {
                Thread.Sleep(10);
            }
        };
    }

    public override void Teardown()
    {
        server.Stop();
        server.Join();
    }

    private void OnDatagram(object sender, PicoFaceTrackingDatagram datagram)
    {
        // TODO
    }

    private PicoBridgeServer server = new();
    private CancellationToken token = new();
    private UnifiedTrackingData convertedData = new();
}
