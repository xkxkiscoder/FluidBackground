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
    private SKBitmap? _cachedBitmap;
    private int _cachedWidth;
    private int _cachedHeight;
    private float _starScale = 1.0f;

    public string Name => "SkiaSharp 2D";
    public bool IsAvailable => _initialized;

    public bool Initialize(FluidConfig config)
    {
        _config = config.Clone();
        _starScale = _config.StarScale;
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

        // 复用位图缓冲区，避免每帧分配
        if (_cachedBitmap == null || _cachedWidth != sw || _cachedHeight != sh)
        {
            _cachedBitmap?.Dispose();
            _cachedBitmap = new SKBitmap(sw, sh, SKColorType.Rgba8888, SKAlphaType.Premul);
            _cachedWidth = sw;
            _cachedHeight = sh;
        }

        var time = (float)timeSeconds * _config.Speed;
        float aspect = (float)width / height;

        // 预读取指针位置到局部变量，避免并行循环中的竞态
        float pX = _pointerX;
        float pY = _pointerY;
        bool enablePointer = _config.EnablePointerInteraction;
        float pointerRadius = _config.PointerRadius;
        FluidMode mode = _config.Mode;

        unsafe
        {
            var ptr = (uint*)_cachedBitmap.GetPixels();
            var stride = _cachedBitmap.RowBytes / 4;

            // 并行化像素计算：每行写入独立内存区域，无数据竞争
            System.Threading.Tasks.Parallel.For(0, sh, y =>
            {
                float v = (float)y / sh;
                for (int x = 0; x < sw; x++)
                {
                    float u = (float)x / sw;
                    ptr[y * stride + x] = CalculatePixel(u, v, time, pX, pY, enablePointer, pointerRadius, mode, aspect);
                }
            });
        }

        using var image = SKImage.FromBitmap(_cachedBitmap);
        canvas.DrawImage(image,
            new SKRect(0, 0, sw, sh),
            new SKRect(0, 0, width, height),
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
    }

    private uint CalculatePixel(float u, float v, float time, float pX, float pY, bool enablePointer, float pointerRadius, FluidMode mode, float aspect)
    {
        float3 color = mode switch
        {
            FluidMode.Fluid => FluidEffect(u, v, time),
            FluidMode.Starfield => StarfieldEffect(u, v, time, aspect),
            FluidMode.Nebula => NebulaEffect(u, v, time, pX, pY),
            FluidMode.Aurora => AuroraEffect(u, v, time, pX, pY),
            _ => FluidEffect(u, v, time)
        };

        if (enablePointer)
        {
            float dx = u - pX;
            float dy = v - pY;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            float inf = SmoothStep(pointerRadius, 0, dist) * 0.3f;
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

    private float3 StarfieldEffect(float u, float v, float t, float aspect)
    {
        float density = Math.Clamp(_config!.Density, 0f, 1f);

        // 深空背景（径向渐暗，更深的夜空基调）
        float radialU = (u - 0.5f) * aspect;
        float radial = MathF.Sqrt(radialU * radialU + (v - 0.5f) * (v - 0.5f)) * 1.6f;
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
            color += nebulaColor * (0.8f * density);
        }

        // 星点（两层，密度与大小可控）
        float driftX = t * 0.008f;
        float driftY = t * 0.005f;
        color += new float3(1f, 1f, 1f) * (StarLayer(u - driftX, v - driftY, 12f, t, density, aspect, _starScale) * 0.9f);
        color += new float3(0.8f, 0.85f, 1f) * (StarLayer((u - driftX) * 1.7f, (v - driftY) * 1.7f, 20f, t, density, aspect, _starScale) * 0.6f);

        // 流星
        if (_config.EnableMeteor)
        {
            color += new float3(1f, 1f, 1f) * Meteor(u, v, t);
        }

        return color;
    }

    /// <summary>
    /// 星云胶囊效果（优化版：减少噪声层数，使用更快的算法）
    /// </summary>
    /// <param name="u">归一化X坐标（0-1）</param>
    /// <param name="v">归一化Y坐标（0-1）</param>
    /// <param name="t">时间参数</param>
    /// <param name="pX">指针X坐标</param>
    /// <param name="pY">指针Y坐标</param>
    /// <returns>像素颜色</returns>
    private float3 NebulaEffect(float u, float v, float t, float pX, float pY)
    {
        var c = _config!.Colors;
        var c0 = ToFloat3(c[0]);
        var c1 = ToFloat3(c.Length > 1 ? c[1] : c[0]);
        var c2 = ToFloat3(c.Length > 2 ? c[2] : c[0]);
        var c3 = ToFloat3(c.Length > 3 ? c[3] : c[0]);

        float seed = _config.Seed;
        float px = u - 0.5f;
        float py = v - 0.5f;
        float distanceToPointer = MathF.Sqrt((px - pX) * (px - pX) + (py - pY) * (py - pY));

        // 域扭曲（简化计算）
        float influence = MathF.Exp(-distanceToPointer * 4.6f) * 0.5f;
        float angle = influence * 1.7f;
        float cosA = MathF.Cos(angle);
        float sinA = MathF.Sin(angle);
        float newPx = cosA * px - sinA * py;
        float newPy = sinA * px + cosA * py;
        px = newPx;
        py = newPy;

        // 使用优化的3层fbm（减少计算量）
        float q1 = FastFbm(px * 1.35f + t * 0.22f + seed, py * 1.35f - t * 0.13f + seed);
        float q2 = FastFbm(px * 1.35f + 5.2f + t * 0.22f, py * 1.35f + 1.3f - t * 0.13f);
        
        // 简化r层计算（只用1次fbm而不是2次）
        float r = FastFbm(px * 2.0f + 3.0f * q1 + 5.0f + t * 0.10f, py * 2.0f + 3.0f * q2 + 5.0f + t * 0.10f);

        float cloud = FastFbm(px * 1.7f + 4.0f * r, py * 1.7f + 4.0f * r);
        float veins = FastFbm(px * 3.0f - 2.0f * q1 + t * 0.065f, py * 3.0f - 2.0f * q2 + t * 0.065f);
        float nebula = SmoothStep(0.18f, 0.91f, cloud * 0.9f + veins * 0.22f);

        float3 color = NebulaPalette(nebula, c0, c1, c2, c3);
        color += c3 * MathF.Pow(MathF.Max(cloud - 0.63f, 0f), 2f) * 1.05f;
        color *= 0.78f + 0.34f * SmoothStep(0.15f, 0.9f, veins);

        // 星点（简化计算）
        float starX = u + seed * 0.013f;
        float starY = v;
        int starGridX = (int)MathF.Floor(starX * 132f);
        int starGridY = (int)MathF.Floor(starY * 58f);
        float starCellX = (starX * 132f) - starGridX - 0.5f;
        float starCellY = (starY * 58f) - starGridY - 0.5f;
        float starRandom = Hash21(starGridX, starGridY);
        float starShape = SmoothStep(0.075f, 0f, MathF.Sqrt(starCellX * starCellX + starCellY * starCellY));
        float starMask = starRandom > 0.989f ? starShape : 0f;
        float twinkle = 0.35f + 0.65f * MathF.Sin(t * (1f + starRandom * 2.4f) + starRandom * 40f) * 0.5f + 0.5f;
        color += starMask * twinkle * Lerp(c2, c3, starRandom) * 1.05f;

        return color;
    }

    /// <summary>
    /// 极光效果（低频噪声与多条柔光带生成平滑迁移的渐变）
    /// </summary>
    /// <param name="u">归一化X坐标（0-1）</param>
    /// <param name="v">归一化Y坐标（0-1）</param>
    /// <param name="t">时间参数</param>
    /// <param name="pX">指针X坐标</param>
    /// <param name="pY">指针Y坐标</param>
    /// <returns>像素颜色</returns>
    private float3 AuroraEffect(float u, float v, float t, float pX, float pY)
    {
        var c = _config!.Colors;
        var c0 = ToFloat3(c[0]);
        var c1 = ToFloat3(c.Length > 1 ? c[1] : c[0]);
        var c2 = ToFloat3(c.Length > 2 ? c[2] : c[0]);
        var c3 = ToFloat3(c.Length > 3 ? c[3] : c[0]);

        float seed = _config.Seed;
        float distanceToPointer = MathF.Sqrt((u - pX) * (u - pX) + (v - pY) * (v - pY));

        return _config.AuroraProfile switch
        {
            AuroraProfile.Polar => RenderPolar(u, v, t, seed, distanceToPointer, c0, c1, c2, c3),
            AuroraProfile.Dubdot => RenderDubdot(u, v, t, seed, distanceToPointer, c0, c1, c2, c3),
            AuroraProfile.Vercel => RenderVercel(u, v, t, seed, distanceToPointer, c0, c1, c2, c3),
            _ => RenderPolar(u, v, t, seed, distanceToPointer, c0, c1, c2, c3)
        };
    }

    /// <summary>
    /// POLAR极光效果（深色胶囊，橙色、洋红与暖白柔光带）
    /// </summary>
    /// <param name="u">归一化X坐标</param>
    /// <param name="v">归一化Y坐标</param>
    /// <param name="t">时间参数</param>
    /// <param name="seed">随机种子</param>
    /// <param name="distanceToPointer">到指针的距离</param>
    /// <param name="c0">颜色0</param>
    /// <param name="c1">颜色1</param>
    /// <param name="c2">颜色2</param>
    /// <param name="c3">颜色3</param>
    /// <returns>像素颜色</returns>
    private float3 RenderPolar(float u, float v, float t, float seed, float distanceToPointer, float3 c0, float3 c1, float3 c2, float3 c3)
    {
        float phase = t * 1.08f + seed * 0.063f;
        float rightField = SmoothStep(0.06f, 0.96f, u);
        float grain = FastFbm(u * 1.85f - phase * 0.14f, v * 2.45f + phase * 0.10f + seed) - 0.5f;

        float orangeCenter = 0.76f - u * 0.20f + MathF.Sin(phase + u * 3.7f) * 0.14f + grain * 0.16f;
        float magentaCenter = 0.37f + u * 0.13f + MathF.Sin(phase * 0.84f + u * 4.7f + 1.1f) * 0.16f - grain * 0.14f;
        float lowerCenter = 0.13f + MathF.Sin(phase * 0.72f + u * 3.8f) * 0.10f;
        float sweepCenter = 0.54f + MathF.Sin(phase * 1.22f + u * 5.4f) * 0.08f;

        float orangeBand = Gaussian(v, orangeCenter, 0.074f);
        float magentaBand = Gaussian(v, magentaCenter, 0.115f);
        float lowerBand = Gaussian(v, lowerCenter, 0.070f);
        float sweepBand = Gaussian(v, sweepCenter, 0.027f) * SmoothStep(0.28f, 0.98f, u);

        float coreX = 0.945f + MathF.Sin(phase * 0.68f) * 0.052f;
        float coreY = 0.60f + MathF.Cos(phase * 0.83f) * 0.145f;
        float whiteCore = MathF.Exp(-MathF.Sqrt((u - coreX) * (u - coreX) * 2.05f * 2.05f + (v - coreY) * (v - coreY) * 0.94f * 0.94f) * 6.1f);

        float secCoreX = 0.90f + MathF.Cos(phase * 0.47f) * 0.055f;
        float secCoreY = 0.27f + MathF.Sin(phase * 0.64f) * 0.08f;
        float secondaryCore = MathF.Exp(-MathF.Sqrt((u - secCoreX) * (u - secCoreX) * 2.4f * 2.4f + (v - secCoreY) * (v - secCoreY) * 1.15f * 1.15f) * 7.0f);

        float pulse = 0.72f + MathF.Sin(phase * 1.62f) * 0.28f;
        float pointerBend = MathF.Exp(-distanceToPointer * 6.4f) * 0.5f;

        float3 color = c0;
        color = Lerp(color, c1, Math.Clamp(orangeBand * rightField * 1.16f, 0f, 1f));
        color = Lerp(color, c2, Math.Clamp((magentaBand * 1.18f + lowerBand * 0.82f) * rightField, 0f, 1f));
        color += c3 * whiteCore * pulse * 1.30f;
        color += Lerp(c3, c2, 0.30f) * secondaryCore * 0.58f;
        color += Lerp(c3, c2, 0.55f) * sweepBand * 0.42f;
        color += c2 * pointerBend * rightField * 0.24f;
        color += Lerp(c1, c2, 0.55f) * SmoothStep(0.35f, 0.94f, grain + 0.5f) * rightField * 0.18f;

        return color;
    }

    /// <summary>
    /// DUBDOT极光效果（白色胶囊，浅蓝、天蓝与青蓝柔光带）
    /// </summary>
    /// <param name="u">归一化X坐标</param>
    /// <param name="v">归一化Y坐标</param>
    /// <param name="t">时间参数</param>
    /// <param name="seed">随机种子</param>
    /// <param name="distanceToPointer">到指针的距离</param>
    /// <param name="c0">颜色0</param>
    /// <param name="c1">颜色1</param>
    /// <param name="c2">颜色2</param>
    /// <param name="c3">颜色3</param>
    /// <returns>像素颜色</returns>
    private float3 RenderDubdot(float u, float v, float t, float seed, float distanceToPointer, float3 c0, float3 c1, float3 c2, float3 c3)
    {
        float phase = t * 0.86f + seed * 0.051f;
        float rightField = SmoothStep(0.16f, 0.97f, u);
        float drift = FastFbm(u * 1.25f - phase * 0.105f, v * 1.95f + phase * 0.075f + seed) - 0.5f;

        float upperCenter = 0.72f - u * 0.18f + MathF.Sin(phase + u * 3.4f) * 0.115f + drift * 0.15f;
        float lowerCenter = 0.28f + u * 0.11f + MathF.Cos(phase * 0.88f + u * 3.2f) * 0.125f - drift * 0.12f;
        float middleCenter = 0.50f + MathF.Sin(phase * 1.18f + u * 4.8f) * 0.075f;
        float upperBand = Gaussian(v, upperCenter, 0.115f);
        float lowerBand = Gaussian(v, lowerCenter, 0.125f);
        float middleBand = Gaussian(v, middleCenter, 0.052f) * SmoothStep(0.34f, 0.98f, u);

        float softBodyX = 0.87f + MathF.Sin(phase * 0.52f) * 0.065f;
        float softBodyY = 0.51f + MathF.Cos(phase * 0.44f) * 0.045f;
        float softBody = MathF.Exp(-MathF.Sqrt((u - softBodyX) * (u - softBodyX) * 1.42f * 1.42f + (v - softBodyY) * (v - softBodyY) * 0.72f * 0.72f) * 2.85f);

        float pointerBend = MathF.Exp(-distanceToPointer * 6.8f) * 0.5f;

        float3 color = c0;
        color = Lerp(color, c1, Math.Clamp(softBody * rightField * 0.74f, 0f, 1f));
        color = Lerp(color, c2, Math.Clamp((upperBand * 0.76f + middleBand * 0.28f) * rightField, 0f, 1f));
        color = Lerp(color, c3, Math.Clamp((lowerBand * 0.82f + softBody * 0.46f + middleBand * 0.34f) * rightField, 0f, 1f));
        color += Lerp(c2, c3, 0.58f) * middleBand * rightField * 0.18f;
        color = Lerp(color, new float3(1f, 1f, 1f), SmoothStep(0f, 0.34f, 1f - rightField) * 0.24f);
        color += c3 * pointerBend * rightField * 0.15f;

        return color;
    }

    /// <summary>
    /// VERCEL极光效果（白色胶囊，薄荷绿、淡黄与浅粉柔光带）
    /// </summary>
    /// <param name="u">归一化X坐标</param>
    /// <param name="v">归一化Y坐标</param>
    /// <param name="t">时间参数</param>
    /// <param name="seed">随机种子</param>
    /// <param name="distanceToPointer">到指针的距离</param>
    /// <param name="c0">颜色0</param>
    /// <param name="c1">颜色1</param>
    /// <param name="c2">颜色2</param>
    /// <param name="c3">颜色3</param>
    /// <returns>像素颜色</returns>
    private float3 RenderVercel(float u, float v, float t, float seed, float distanceToPointer, float3 c0, float3 c1, float3 c2, float3 c3)
    {
        float phase = t * 1.62f + seed * 0.044f;
        float rightField = SmoothStep(0.12f, 0.97f, u);
        float flowNoise = FastFbm(u * 1.28f - phase * 0.12f, v * 1.92f + phase * 0.09f + seed) - 0.5f;

        float mintCenter = 0.78f - u * 0.24f + MathF.Sin(phase + u * 3.9f) * 0.16f + flowNoise * 0.13f;
        float goldCenter = 0.50f + MathF.Sin(phase * 0.86f + u * 4.5f + 1.7f) * 0.18f - flowNoise * 0.11f;
        float pinkCenter = 0.20f + u * 0.17f + MathF.Sin(phase * 1.08f + u * 3.6f + 3.0f) * 0.15f + flowNoise * 0.10f;

        float mintBand = Gaussian(v, mintCenter, 0.105f);
        float goldBand = Gaussian(v, goldCenter, 0.115f);
        float pinkBand = Gaussian(v, pinkCenter, 0.100f);

        float mintCoreX = 0.88f + MathF.Sin(phase * 0.68f) * 0.085f;
        float mintCoreY = 0.74f + MathF.Cos(phase * 0.82f) * 0.13f;
        float mintCore = MathF.Exp(-MathF.Sqrt((u - mintCoreX) * (u - mintCoreX) * 1.48f * 1.48f + (v - mintCoreY) * (v - mintCoreY) * 0.82f * 0.82f) * 3.35f);

        float goldCoreX = 0.92f + MathF.Cos(phase * 0.61f + 1.2f) * 0.080f;
        float goldCoreY = 0.50f + MathF.Sin(phase * 0.77f) * 0.15f;
        float goldCore = MathF.Exp(-MathF.Sqrt((u - goldCoreX) * (u - goldCoreX) * 1.42f * 1.42f + (v - goldCoreY) * (v - goldCoreY) * 0.80f * 0.80f) * 3.20f);

        float pinkCoreX = 0.86f + MathF.Sin(phase * 0.73f + 2.1f) * 0.095f;
        float pinkCoreY = 0.27f + MathF.Cos(phase * 0.66f) * 0.13f;
        float pinkCore = MathF.Exp(-MathF.Sqrt((u - pinkCoreX) * (u - pinkCoreX) * 1.44f * 1.44f + (v - pinkCoreY) * (v - pinkCoreY) * 0.82f * 0.82f) * 3.28f);

        float rightBodyX = 0.91f + MathF.Sin(phase * 0.38f) * 0.045f;
        float rightBodyY = 0.50f + MathF.Cos(phase * 0.42f) * 0.055f;
        float rightBody = MathF.Exp(-MathF.Sqrt((u - rightBodyX) * (u - rightBodyX) * 1.20f * 1.20f + (v - rightBodyY) * (v - rightBodyY) * 0.68f * 0.68f) * 2.62f);

        float separation = Gaussian(v, 0.49f + MathF.Sin(phase * 0.94f + u * 5.0f) * 0.10f, 0.035f) * SmoothStep(0.34f, 0.98f, u);

        float pointerBend = MathF.Exp(-distanceToPointer * 6.8f) * 0.5f;

        float3 color = c0;
        color = Lerp(color, c1, Math.Clamp((mintBand * 0.86f + mintCore * 0.72f + rightBody * 0.18f) * rightField, 0f, 1f));
        color = Lerp(color, c2, Math.Clamp((goldBand * 0.90f + goldCore * 0.76f + rightBody * 0.16f) * rightField, 0f, 1f));
        color = Lerp(color, c3, Math.Clamp((pinkBand * 0.84f + pinkCore * 0.70f + rightBody * 0.12f) * rightField, 0f, 1f));

        color += c1 * mintBand * rightField * 0.10f;
        color += c2 * goldBand * rightField * 0.11f;
        color += c3 * pinkBand * rightField * 0.10f;
        color = Lerp(color, new float3(1f, 1f, 1f), separation * 0.11f);
        color += Lerp(c1, c3, 0.5f) * pointerBend * rightField * 0.10f;

        return color;
    }

    /// <summary>
    /// 星云分形布朗运动噪声（6层叠加，带旋转变换）
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <returns>噪声值</returns>
    private static float NebulaFbm(float x, float y)
    {
        float value = 0f;
        float amplitude = 0.52f;
        for (int i = 0; i < 6; i++)
        {
            value += amplitude * Noise(x, y);
            float newX = 0.80f * x + 0.60f * y + 17.7f;
            float newY = -0.60f * x + 0.80f * y + 17.7f;
            x = newX * 2.03f;
            y = newY * 2.03f;
            amplitude *= 0.5f;
        }
        return value;
    }

    /// <summary>
    /// 优化的星云分形布朗运动噪声（4层叠加，性能更好）
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <returns>噪声值</returns>
    private static float NebulaFbmOptimized(float x, float y)
    {
        float value = 0f;
        float amplitude = 0.5f;
        for (int i = 0; i < 4; i++)
        {
            value += amplitude * Noise(x, y);
            float newX = 0.80f * x + 0.60f * y + 17.7f;
            float newY = -0.60f * x + 0.80f * y + 17.7f;
            x = newX * 2.0f;
            y = newY * 2.0f;
            amplitude *= 0.5f;
        }
        return value;
    }

    /// <summary>
    /// 快速fbm（3层叠加，最高性能）
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <returns>噪声值</returns>
    private static float FastFbm(float x, float y)
    {
        float value = 0f;
        float amplitude = 0.5f;
        for (int i = 0; i < 3; i++)
        {
            value += amplitude * Noise(x, y);
            x = x * 2.0f + 17.7f;
            y = y * 2.0f + 17.7f;
            amplitude *= 0.5f;
        }
        return value;
    }

    /// <summary>
    /// 星云调色板（在4个颜色之间平滑插值）
    /// </summary>
    /// <param name="t">插值参数（0-1）</param>
    /// <param name="c0">颜色0</param>
    /// <param name="c1">颜色1</param>
    /// <param name="c2">颜色2</param>
    /// <param name="c3">颜色3</param>
    /// <returns>插值后的颜色</returns>
    private static float3 NebulaPalette(float t, float3 c0, float3 c1, float3 c2, float3 c3)
    {
        t = Math.Clamp(t, 0f, 1f);
        float3 shadow = Lerp(c0, c1, SmoothStep(0.06f, 0.62f, t));
        float3 body = Lerp(c1, c2, SmoothStep(0.30f, 0.82f, t));
        float3 highlight = Lerp(c2, c3, SmoothStep(0.74f, 1f, t));
        float3 restrained = Lerp(shadow, body, SmoothStep(0.26f, 0.72f, t));
        return Lerp(restrained, highlight, SmoothStep(0.78f, 0.97f, t));
    }

    /// <summary>
    /// 高斯函数（用于生成柔光带）
    /// </summary>
    /// <param name="value">输入值</param>
    /// <param name="center">中心值</param>
    /// <param name="width">宽度</param>
    /// <returns>高斯值</returns>
    private static float Gaussian(float value, float center, float width)
    {
        return MathF.Exp(-(value - center) * (value - center) / MathF.Max(width, 0.0001f));
    }

    /// <summary>
    /// 2D哈希函数（用于生成随机数）
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <returns>哈希值（0-1）</returns>
    private static float Hash21(int x, int y)
    {
        float px = (x % 1000) / 1000f;
        float py = (y % 1000) / 1000f;
        px = (px * 123.34f) % 1f;
        py = (py * 456.21f) % 1f;
        float dot = px * (px + 45.32f) + py * (py + 45.32f);
        return (dot * px * py) % 1f;
    }

    private static float StarLayer(float x, float y, float scale, float t, float density, float aspect, float starScale)
    {
        float px = x * scale;
        float py = y * scale;
        int cellX = (int)MathF.Floor(px);
        int cellY = (int)MathF.Floor(py);
        float fx = px - cellX;
        float fy = py - cellY;

        // 出现概率随密度（密度越高出星越多）
        if (Hash(cellX + 3, cellY + 3) > (1f - density * 0.18f)) return 0f;

        float r1 = Hash(cellX, cellY);
        float r2 = Hash(cellX + 1, cellY);
        float r3 = Hash(cellX, cellY + 1);
        float r4 = Hash(cellX + 1, cellY + 1);
        float r5 = Hash(cellX + 2, cellY + 2);

        // 宽高比校正：让星星在屏幕上显示为正圆
        float dx = (fx - r1) * aspect;
        float dy = fy - r2;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        float size = (0.01f + 0.025f * r3) * starScale;   // 星星大小可控
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

    public void UpdateConfig(FluidConfig config)
    {
        _config = config.Clone();
        _starScale = config.StarScale;
    }

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
        _cachedBitmap?.Dispose();
        _cachedBitmap = null;
        _initialized = false;
        GC.SuppressFinalize(this);
    }
}
