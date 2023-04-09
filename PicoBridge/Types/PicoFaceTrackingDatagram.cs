namespace PicoBridge.Types;

/// <summary>
/// It's C counterpart:
///
/// <code>
/// typedef struct {
///     int64_t timestamp;
///     float blendShapeWeight[72];
///     float videoInputValid[10];
///     float laughingProb;
///     float emotionProb[10];
///     float reserved[128];
/// } PxrFTInfo;
/// </code>
/// </summary>
public sealed class PicoFaceTrackingDatagram
{
    public PicoFaceTrackingDatagram(Stream data)
    {
        // TODO
        
    }

    public long Timestamp { get; } = 0;
    public float[] BlendShapeWeight { get; } = new float[72];
    public float LaughingProbability { get; } = 0;
    public float[] VideoInputValidity { get; } = new float[10];
    public float[] EmotionProbability { get; } = new float[10];
}