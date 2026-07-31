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

    public void RenderToBitmap(double timeSeconds, int width, int height, SKBitmap target)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("渲染器未初始化或不可用");

        using var canvas = new SKCanvas(target);
        RenderToCanvas(canvas, timeSeconds, width, height);
        canvas.Flush();
    }

    public void RenderToCanvas(SKCanvas canvas, double timeSeconds, int width, int height)
    {
        if (!IsAvailable || _config == null)
            throw new InvalidOperationException("渲染器未初始化或不可用");

        // 星空模式含高频细节（星点/流星），降分辨率后拉伸会糊，故始终全分辨率渲染
        float quality = _config.Mode == FluidMode.Starfield ? 1.0f : Math.Max(1.0f, _config.RenderQuality);
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

        using var image = SKImage.FromBitmap(smallBitmap);
        canvas.DrawImage(image,
            new SKRect(0, 0, image.Width, image.Height),
            new SKRect(0, 0, width, height),
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
    }

    private uint CalculatePixel(float u, float v, float time)
    {
        float3 color = _config!.Mode switch
        {
            FluidMode.Fluid => FluidEffect(u, v, time),
            FluidMode.Starfield => StarfieldEffect(u, v, time),
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
        return (uint)(0xFF000000 | ((uint)b << 16) | ((uint)g << 8) | r);
    }

    private float3 FluidEffect(float u, float v, float t)
    {
        var c = _config!.Colors;
        float density = Math.Clamp(_config.Density, 0f, 1f);
        float n1 = Noise(u * 3 + t * 0.3f, v * 3 + t * 0.2f);
        float n2 = Noise(u * 2 - t * 0.2f, v * 2 + t * 0.3f);
        float n3 = Noise(u * 4 + t * 0.15f, v * 4 - t * 0.25f);
        float blend = (n1 + n2 + n3) * 0.33f;

        int idx = (int)(blend * (c.Length - 1));
        idx = Math.Clamp(idx, 0, c.Length - 2);
        float frac = blend * (c.Length - 1) - idx;

        float3 color = Lerp(ToFloat3(c[idx]), ToFloat3(c[idx + 1]), frac);

        // 浓度：低 → 色彩饱和度降低（向平均色收敛，但保持原亮度，避免整体变亮/变暗）
        var mid = new float3(0f, 0f, 0f);
        for (int i = 0; i < c.Length; i++)
            mid += ToFloat3(c[i]);
        mid = mid * (1f / c.Length);
        var lowSat = Lerp(mid, color, 0.35f + 0.65f * density);
        float lum = 0.2126f * color.X + 0.7152f * color.Y + 0.0722f * color.Z;
        float lowSatLum = 0.2126f * lowSat.X + 0.7152f * lowSat.Y + 0.0722f * lowSat.Z;
        return lowSat * (lum / MathF.Max(lowSatLum, 1e-4f));
    }

    private float3 StarfieldEffect(float u, float v, float t)
    {
        float density = Math.Clamp(_config!.Density, 0f, 1f);

        // 深空背景（径向渐暗，更深的夜空基调）
        float radial = MathF.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f)) * 1.6f;
        float3 color = Lerp(new float3(0.03f, 0.04f, 0.10f), new float3(0f, 0f, 0f), SmoothStep(0.2f, 1.2f, radial));

        // 彩色星云（频率固定，浓度控制浓郁程度）
        if (_config.EnableNebula)
        {
            var c = _config!.Colors;
            var c0 = ToFloat3(c[0]);
            var c1 = ToFloat3(c.Length > 1 ? c[1] : c[0]);
            var c2 = ToFloat3(c.Length > 2 ? c[2] : c[0]);
            var c3 = ToFloat3(c.Length > 3 ? c[3] : c[0]);
            float rn = Noise(u * 2f + t * 0.02f, v * 2f + t * 0.015f);
            float gn = Noise(u * 2f + t * 0.018f + 1.7f, v * 2f + t * 0.02f + 9.2f);
            float bn = Noise(u * 2f + t * 0.021f + 8.3f, v * 2f + t * 0.024f + 2.8f);
            var nebulaColor = Lerp(c0, c1, rn);
            nebulaColor = Lerp(nebulaColor, c2, gn);
            nebulaColor = Lerp(nebulaColor, c3, bn);
            color += nebulaColor * (0.25f * density);
        }

        // 星点（两层，网格固定，浓度控制出现概率；亮度恒定）
        float driftX = t * 0.008f;
        float driftY = t * 0.005f;
        color += new float3(1f, 1f, 1f) * (StarLayer(u - driftX, v - driftY, 12f, t, density) * 0.9f);
        color += new float3(0.8f, 0.85f, 1f) * (StarLayer((u - driftX) * 1.7f, (v - driftY) * 1.7f, 20f, t, density) * 0.6f);

        // 流星
        if (_config.EnableMeteor)
        {
            color += new float3(1f, 1f, 1f) * Meteor(u, v, t);
        }

        return color;
    }

    private static float StarLayer(float x, float y, float scale, float t, float density)
    {
        float px = x * scale;
        float py = y * scale;
        int cellX = (int)MathF.Floor(px);
        int cellY = (int)MathF.Floor(py);
        float fx = px - cellX;
        float fy = py - cellY;

        // 出现概率随浓度（浓度越高出星越多）
        if (Hash(cellX + 3, cellY + 3) > 0.9f * density) return 0f;

        float r1 = Hash(cellX, cellY);
        float r2 = Hash(cellX + 1, cellY);
        float r3 = Hash(cellX, cellY + 1);
        float r4 = Hash(cellX + 1, cellY + 1);
        float r5 = Hash(cellX + 2, cellY + 2);

        float dist = MathF.Sqrt((fx - r1) * (fx - r1) + (fy - r2) * (fy - r2));
        float size = 0.01f + 0.025f * r3;   // 中等大小、边缘柔和
        float brightness = 0.3f + 0.6f * r4;
        float twinkle = 0.5f + 0.5f * MathF.Sin(t * (1.5f + r5 * 2.5f) + r5 * 6.283f);

        return SmoothStep(size, 0f, dist) * brightness * (0.4f + 0.6f * twinkle);
    }

    private static float Meteor(float u, float v, float t)
    {
        const float cycle = 12f;
        float seed = MathF.Floor(t / cycle);
        float phase = t / cycle - seed;

        // 每颗流星随机存在时长（1.5 ~ 3.5 秒）
        float durFrac = (1.5f + 2f * Hash((int)seed, 2)) / cycle;
        if (phase > durFrac) return 0f;

        float sx = Hash((int)seed, 0) * 0.7f;
        float sy = Hash((int)seed, 1) * 0.4f;
        float invLen = 1f / MathF.Sqrt(0.8f * 0.8f + 1f);
        float dx = 0.8f * invLen;
        float dy = 1f * invLen;   // 左上 → 右下

        float life = phase / durFrac;
        float headX = sx + dx * life * 1.2f;
        float headY = sy + dy * life * 1.2f;

        float toX = u - headX;
        float toY = v - headY;
        float proj = toX * (-dx) + toY * (-dy);
        float cp = Math.Clamp(proj, 0f, 0.35f);
        float distToLine = MathF.Sqrt((toX + dx * cp) * (toX + dx * cp) + (toY + dy * cp) * (toY + dy * cp));

        float tail = MathF.Exp(-distToLine * 140f) * MathF.Exp(-proj * 8f) * 0.8f;
        float headGlow = MathF.Exp(-MathF.Sqrt(toX * toX + toY * toY) * 240f) * 0.9f;
        float visible = SmoothStep(0f, 0.15f, life) * (1f - SmoothStep(0.7f, 1f, life));

        return (tail + headGlow) * visible;
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

    private record struct float3(float X, float Y, float Z)
    {
        public static float3 operator +(float3 a, float3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static float3 operator *(float3 a, float b) => new(a.X * b, a.Y * b, a.Z * b);
        public static float3 operator *(float b, float3 a) => new(a.X * b, a.Y * b, a.Z * b);
    }

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
