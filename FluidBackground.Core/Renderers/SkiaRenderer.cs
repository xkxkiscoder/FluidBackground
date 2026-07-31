using FluidBackground.Core.Models;
using SkiaSharp;

namespace FluidBackground.Core.Renderers;

/// <summary>
/// 基于SkiaSharp的2D流体渲染器
/// </summary>
public class SkiaRenderer : IFluidRenderer
{
    private FluidConfig? _config;
    private float _pointerX = 0.5f;
    private float _pointerY = 0.5f;
    private bool _initialized;

    public string Name => "SkiaSharp 2D";
    public bool IsAvailable => _initialized;

    public bool Initialize(FluidConfig config)
    {
        _config = config.Clone();
        _initialized = true;
        return true;
    }

    public byte[] RenderFrame(double timeSeconds, int width, int height)
    {
        using var bitmap = RenderToBitmap(timeSeconds, width, height);
        return GetBitmapBytes(bitmap);
    }

    public SKBitmap RenderToBitmap(double timeSeconds, int width, int height)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("渲染器未初始化或不可用");

        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        RenderToCanvas(canvas, timeSeconds, width, height);
        canvas.Flush();
        return bitmap;
    }

    public void RenderToCanvas(SKCanvas canvas, double timeSeconds, int width, int height)
    {
        if (!IsAvailable || _config == null)
            throw new InvalidOperationException("渲染器未初始化或不可用");

        float quality = Math.Max(1.0f, _config.RenderQuality);
        int sw = Math.Max(1, (int)(width / quality));
        int sh = Math.Max(1, (int)(height / quality));

        using var smallBitmap = new SKBitmap(sw, sh, SKColorType.Rgba8888, SKAlphaType.Premul);
        var time = (float)timeSeconds * _config.Speed;

        unsafe
        {
            var ptr = (uint*)smallBitmap.GetPixels();
            var stride = smallBitmap.RowBytes / 4;

            for (int y = 0; y < sh; y++)
            {
                for (int x = 0; x < sw; x++)
                {
                    float u = (float)x / sw;
                    float v = (float)y / sh;
                    ptr[y * stride + x] = CalculatePixel(u, v, time);
                }
            }
        }

        canvas.DrawBitmap(smallBitmap, new SKRect(0, 0, width, height));
    }

    private uint CalculatePixel(float u, float v, float time)
    {
        float3 color = _config!.Mode switch
        {
            FluidMode.Fluid => FluidEffect(u, v, time),
            FluidMode.Ripple => RippleEffect(u, v, time),
            FluidMode.Breathing => BreathingEffect(u, v, time),
            _ => FluidEffect(u, v, time)
        };

        if (_config.EnablePointerInteraction)
        {
            float dx = u - _pointerX;
            float dy = v - _pointerY;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            float inf = SmoothStep(_config.PointerRadius, 0, dist) * 0.3f;
            color = new float3(
                color.X * (1 + inf),
                color.Y * (1 + inf),
                color.Z * (1 + inf));
        }

        byte r = (byte)(Math.Clamp(color.X, 0, 1) * 255);
        byte g = (byte)(Math.Clamp(color.Y, 0, 1) * 255);
        byte b = (byte)(Math.Clamp(color.Z, 0, 1) * 255);
        return (uint)(0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | b);
    }

    private float3 FluidEffect(float u, float v, float t)
    {
        var c = _config!.Colors;
        float n1 = Noise(u * 3 + t * 0.3f, v * 3 + t * 0.2f);
        float n2 = Noise(u * 2 - t * 0.2f, v * 2 + t * 0.3f);
        float n3 = Noise(u * 4 + t * 0.15f, v * 4 - t * 0.25f);
        float blend = (n1 + n2 + n3) * 0.33f;

        int idx = (int)(blend * (c.Length - 1));
        idx = Math.Clamp(idx, 0, c.Length - 2);
        float frac = blend * (c.Length - 1) - idx;

        return Lerp(ToFloat3(c[idx]), ToFloat3(c[idx + 1]), frac);
    }

    private float3 RippleEffect(float u, float v, float t)
    {
        var c = _config!.Colors;
        float dx = u - 0.5f;
        float dy = v - 0.5f;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        float wave = (MathF.Sin(dist * 20 - t * 3) * 0.5f + 0.5f) * MathF.Exp(-dist * 3);

        float angle = MathF.Atan2(dy, dx);
        float spiral = MathF.Sin(angle * 3 + dist * 10 - t * 2) * 0.5f + 0.5f;
        float blend = (wave * 0.7f + spiral * 0.3f) * _config.Intensity;

        int idx = (int)(blend * (c.Length - 1));
        idx = Math.Clamp(idx, 0, c.Length - 2);
        float frac = blend * (c.Length - 1) - idx;

        return Lerp(ToFloat3(c[idx]), ToFloat3(c[idx + 1]), frac);
    }

    private float3 BreathingEffect(float u, float v, float t)
    {
        var c = _config!.Colors;
        float breath = MathF.Sin(t * 0.5f) * 0.5f + 0.5f;
        float dx = u - 0.5f;
        float dy = v - 0.5f;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        float pulse = SmoothStep(breath + 0.3f, breath - 0.3f, dist);
        float glow = MathF.Exp(-dist * 2) * breath;
        float blend = (pulse * 0.6f + glow * 0.4f) * _config.Intensity;

        int idx = (int)(blend * (c.Length - 1));
        idx = Math.Clamp(idx, 0, c.Length - 2);
        float frac = blend * (c.Length - 1) - idx;

        return Lerp(ToFloat3(c[idx]), ToFloat3(c[idx + 1]), frac);
    }

    private static float Noise(float x, float y)
    {
        int ix = (int)MathF.Floor(x);
        int iy = (int)MathF.Floor(y);
        float fx = x - ix;
        float fy = y - iy;
        fx = fx * fx * (3 - 2 * fx);
        fy = fy * fy * (3 - 2 * fy);

        float a = Hash(ix, iy);
        float b = Hash(ix + 1, iy);
        float c = Hash(ix, iy + 1);
        float d = Hash(ix + 1, iy + 1);

        return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy;
    }

    private static float Hash(int x, int y)
    {
        int h = x * 374761393 + y * 668265263;
        h = (h ^ (h >> 13)) * 1274126177;
        return (h & 0x7fffffff) / (float)0x7fffffff;
    }

    private static float SmoothStep(float e0, float e1, float x)
    {
        float t = Math.Clamp((x - e0) / (e1 - e0), 0, 1);
        return t * t * (3 - 2 * t);
    }

    private static float3 Lerp(float3 a, float3 b, float t) =>
        new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t);

    private static float3 ToFloat3(FluidColor c) => new(c.R, c.G, c.B);

    private record struct float3(float X, float Y, float Z);

    public void UpdateConfig(FluidConfig config) => _config = config.Clone();

    public void SetPointerPosition(float x, float y)
    {
        _pointerX = Math.Clamp(x, 0, 1);
        _pointerY = Math.Clamp(y, 0, 1);
    }

    private static byte[] GetBitmapBytes(SKBitmap bitmap)
    {
        var bytes = new byte[bitmap.Info.BytesSize];
        System.Runtime.InteropServices.Marshal.Copy(bitmap.GetPixels(), bytes, 0, bytes.Length);
        return bytes;
    }

    public void Dispose()
    {
        _initialized = false;
        GC.SuppressFinalize(this);
    }
}
