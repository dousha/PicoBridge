using System;
using System.IO;

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
    private readonly byte[] buffer = new byte[8];

    public PicoFaceTrackingDatagram()
    {
        Timestamp = 0;
        for (var i = 0; i < 72; i++)
        {
            BlendShapeWeight[i] = 0;
        }

        LaughingProbability = 0;

        for (var i = 0; i < 10; i++)
        {
            VideoInputValidity[i] = 0;
            EmotionProbability[i] = 0;
        }
    }

    public PicoFaceTrackingDatagram(Stream data)
    {
        Timestamp = ReadLong(data);
        for (var i = 0; i < 72; i++)
        {
            BlendShapeWeight[i] = ReadFloat(data);
        }

        for (var i = 0; i < 10; i++)
        {
            VideoInputValidity[i] = ReadFloat(data);
        }

        LaughingProbability = ReadFloat(data);

        for (var i = 0; i < 10; i++)
        {
            EmotionProbability[i] = ReadFloat(data);
        }

        data.Seek(128 * 4, SeekOrigin.Current); // discard reserved section
    }

    public long Timestamp { get; }
    public float[] BlendShapeWeight { get; } = new float[72];
    public float LaughingProbability { get; }
    public float[] VideoInputValidity { get; } = new float[10];
    public float[] EmotionProbability { get; } = new float[10];

    public float this[PicoBlendShapeWeight key] => BlendShapeWeight[(int) key];

    private long ReadLong(Stream data)
    {
        if (data.Read(buffer, 0, 8) != 8)
        {
            throw new InvalidDataException("Datagram too short");
        }

        return BitConverter.ToInt64(buffer, 0);
    }

    private float ReadFloat(Stream data)
    {
        if (data.Read(buffer, 0, 4) != 4)
        {
            throw new InvalidDataException("Datagram too short");
        }

        return BitConverter.ToSingle(buffer, 0);
    }
}
