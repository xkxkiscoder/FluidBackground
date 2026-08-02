using FluidBackground.Core.Models;
using SkiaSharp;

namespace FluidBackground.Core.Renderers;

/// <summary>
/// 闪电电弧进度边界渲染器（Skia 矢量实现，跨平台纯渲染，不依赖 UI 框架）
/// <para>
/// 渲染一张深色圆角卡片，内部有一条随进度移动的闪电状发光边界，
/// 包含自然抖动、拖拽扰动、能量残留、充能完成脉冲、主题颜色平滑过渡等动态行为。
/// </para>
/// </summary>
public class LightningArcRenderer : IDisposable
{
    private LightningArcConfig _config;

    // —— 进度与拖拽状态 ——
    private float _targetProgress;
    private float _displayProgress;
    private float _progressVelocity;
    private bool _dragging;
    private float _jitter;

    // —— 时间状态 ——
    private float _elapsed;
    private float _lastTime;
    private bool _hasLastTime;

    // —— 颜色过渡状态 ——
    private FluidColor _currentGlow;
    private FluidColor _currentBackground;
    private FluidColor _currentAmbient;

    // —— 充能完成状态 ——
    private bool _wasComplete;
    private float _completeTime;

    // —— 能量残留（历史闪电路径） ——
    private sealed class TrailFrame
    {
        public TrailFrame(float[] xs, float life)
        {
            Xs = xs;
            Life = life;
        }

        public float[] Xs { get; }
        public float Life { get; set; }
    }

    private readonly Queue<TrailFrame> _trail = new();

    // —— 缓存资源（随高度重建） ——
    private SKBitmap? _noiseBitmap;
    private SKPaint? _noisePaint;
    private SKPath? _clipPath;
    private SKRect _clipRect;
    private float _clipRadius;
    private readonly SKTypeface? _boldTypeface;
    private readonly SKTypeface? _regularTypeface;
    private float _cacheHeight = -1f;
    private SKImageFilter? _outerBlur;
    private SKImageFilter? _midBlur;
    private SKPaint? _outerPaint;
    private SKPaint? _midPaint;
    private SKPaint? _corePaint;
    private SKPaint? _forkPaint;
    private SKPaint? _forkMidPaint;
    private SKPaint? _trailPaint;
    private SKPaint? _shadowPaint;
    private SKFont? _titleFont;
    private SKFont? _subFont;
    private SKFont? _pctFont;

    // —— 方向性拖影状态 ——
    private float _lastBaseX = float.NaN;
    private float _shadowSpeed;

    /// <summary>
    /// 创建渲染器实例
    /// </summary>
    public LightningArcRenderer(LightningArcConfig? config = null)
    {
        _config = config?.Clone() ?? new LightningArcConfig();
        _targetProgress = Math.Clamp(_config.Progress, 0f, 1f);
        _displayProgress = _targetProgress;
        _jitter = _config.JitterAmount;

        _currentGlow = _config.EffectiveGlowColor;
        _currentBackground = _config.EffectiveBackgroundColor;
        _currentAmbient = _config.EffectiveAmbientColor;

        _boldTypeface = SKTypeface.FromFamilyName(null, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        _regularTypeface = SKTypeface.FromFamilyName(null, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
    }

    /// <summary>
    /// 渲染器名称
    /// </summary>
    public string Name => "LightningArc (Skia)";

    /// <summary>
    /// 目标进度（0.0-1.0）
    /// </summary>
    public float TargetProgress => _targetProgress;

    /// <summary>
    /// 当前显示进度（0.0-1.0，弹性插值后的值，用于外部显示）
    /// </summary>
    public float DisplayProgress => _displayProgress;

    /// <summary>
    /// 是否正在拖拽
    /// </summary>
    public bool IsDragging => _dragging;

    /// <summary>
    /// 设置目标进度（0.0-1.0），显示进度会以弹性插值追赶
    /// </summary>
    public void SetTargetProgress(float progress)
    {
        _targetProgress = Math.Clamp(progress, 0f, 1f);
    }

    /// <summary>
    /// 设置拖拽状态：拖拽时抖动幅度临时增大，释放后 0.5 秒衰减回原值
    /// </summary>
    public void SetDragging(bool dragging)
    {
        if (_dragging == dragging)
            return;
        _dragging = dragging;
        if (dragging)
        {
            // 拖拽开始：显示进度直接跳到目标，避免弹簧滞后造成"边界跟不上手"
            _displayProgress = _targetProgress;
            _progressVelocity = 0f;
        }
    }

    /// <summary>
    /// 更新配置
    /// <para>注意：会同时把目标进度重置为 <paramref name="config"/>.Progress（含拖拽过程中），
    /// 因此外部改配置时应带上期望的目标进度。</para>
    /// </summary>
    public void UpdateConfig(LightningArcConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config.Clone();
        _targetProgress = Math.Clamp(_config.Progress, 0f, 1f);
    }

    /// <summary>
    /// 渲染到 SKBitmap
    /// </summary>
    public SKBitmap RenderToBitmap(double timeSeconds, int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("宽度和高度必须大于0");

        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        RenderToCanvas(canvas, timeSeconds, width, height);
        canvas.Flush();
        return bitmap;
    }

    /// <summary>
    /// 渲染到现有 SKBitmap
    /// </summary>
    public void RenderToBitmap(double timeSeconds, int width, int height, SKBitmap target)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("宽度和高度必须大于0");

        using var canvas = new SKCanvas(target);
        RenderToCanvas(canvas, timeSeconds, width, height);
        canvas.Flush();
    }

    /// <summary>
    /// 渲染一帧到 SKCanvas
    /// </summary>
    public void RenderToCanvas(SKCanvas canvas, double timeSeconds, int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        // —— 更新动画状态 ——
        float dt = UpdateTime((float)timeSeconds);
        UpdateProgress(dt);
        UpdateJitter(dt);
        UpdateColors(dt);

        // —— 准备几何 ——
        var rect = new SKRect(0, 0, width, height);
        float cornerRadius = Math.Clamp(_config.CornerRadius, 1f, MathF.Max(1f, MathF.Min(width, height) * 0.4f));
        float padX = 12f;
        float baseX = padX + _displayProgress * (width - padX * 2f);
        float intensity = Math.Clamp(_config.GlowIntensity, 0f, 3f);

        // 充能完成脉冲
        float pulse = UpdateCompletionPulse();

        // 闪电路径（当前帧）
        float stepY = MathF.Max(3f, height / 72f);
        int pointCount = Math.Max(2, (int)((height - 2f * padX) / stepY) + 1);
        var xs = new float[pointCount];
        var ys = new float[pointCount];
        float amp = _jitter * width * 2f;   // 抖动幅度（像素）
        int frameSeed = (int)(_elapsed * 60f);
        int forkCellSize = Math.Max(18, (int)(height / 5f));

        for (int i = 0; i < pointCount; i++)
        {
            float y = padX + i * stepY;
            float n = Noise1(y * 0.035f + _elapsed * 1.8f) * 2f - 1f;
            float n2 = Noise1(y * 0.012f - _elapsed * 0.9f) * 2f - 1f;
            xs[i] = baseX + n * amp + n2 * amp * 0.6f;
            ys[i] = y;
        }

        // —— 分叉线段（独立短水平线，每帧按种子变化，闪烁 1-2 帧） ——
        var forks = new List<(float X, float Y, float Length, float Lift)>();
        if (_config.ForkChance > 0f && pointCount > 2)
        {
            int candidateCount = pointCount / 3;
            for (int c = 1; c < candidateCount; c++)
            {
                int idx = c * 3;
                int cell = (int)(ys[idx] / forkCellSize);
                float chance = Hash1(cell * 131 + frameSeed);
                if (chance >= _config.ForkChance)
                    continue;

                float len = (5f + 8f * Hash1(cell * 17 + frameSeed * 3)) * (height / 80f);
                float dir = Hash1(cell * 29 + frameSeed * 5) < 0.5f ? -1f : 1f;
                float lift = (Hash1(cell * 37 + frameSeed * 7) - 0.5f) * 6f;
                forks.Add((xs[idx], ys[idx], len * dir, lift));
            }
        }

        // —— 记录残留帧 ——
        _trail.Enqueue(new TrailFrame(xs, 1f));
        while (_trail.Count > 14)
            _trail.Dequeue();
        foreach (var frame in _trail)
        {
            frame.Life -= dt / 0.45f;
        }
        while (_trail.Count > 0 && _trail.Peek().Life <= 0f)
            _trail.Dequeue();

        // —— 缓存绘制资源（按高度重建） ——
        EnsureCachedResources(width, height, cornerRadius, intensity);
        float flicker = 0.72f + 0.28f * MathF.Sin(_elapsed * 47f) * (0.7f + 0.3f * Noise1(_elapsed * 7f));

        using var path = new SKPath();
        path.MoveTo(xs[0], ys[0]);
        for (int i = 1; i < pointCount; i++)
            path.LineTo(xs[i], ys[i]);

        // 每帧只更新颜色（alpha 闪烁/脉冲）
        _outerPaint!.Color = GlowAlpha(0.30f * intensity * flicker);
        _midPaint!.Color = GlowAlpha(0.55f * intensity * flicker);
        _corePaint!.Color = CoreAlpha(1f);
        _forkPaint!.Color = CoreAlpha(0.9f * flicker);
        _forkMidPaint!.Color = GlowAlpha(0.5f * intensity * flicker);

        // —— 绘制 ——
        canvas.Clear(SKColors.Transparent);
        canvas.Save();
        canvas.ClipPath(_clipPath, SKClipOperation.Intersect, true);

        DrawCardBackground(canvas, rect, intensity);
        DrawAmbientGlow(canvas, baseX, height, intensity);
        DrawTrail(canvas, height, intensity, pulse);
        DrawCometTail(canvas, xs, ys, baseX, amp, intensity, flicker, dt);
        DrawForkGlow(canvas, forks, _forkMidPaint);
        DrawPathGlow(canvas, path, _outerPaint, _midPaint);
        DrawForks(canvas, forks, _forkPaint);
        DrawCorePath(canvas, path, _corePaint);
        DrawTexts(canvas, width, height, intensity, pulse);

        canvas.Restore();
    }

    // ==================== 状态更新 ====================

    private float UpdateTime(float time)
    {
        float dt;
        if (!_hasLastTime)
        {
            dt = 0f;
            _hasLastTime = true;
        }
        else
        {
            dt = time - _lastTime;
        }
        if (dt < 0f || dt > 0.05f)
            dt = 0.016f;
        _lastTime = time;
        _elapsed += dt;
        return dt;
    }

    private void UpdateProgress(float dt)
    {
        if (dt <= 0f)
            return;

        // 拖拽时更跟手（近临界阻尼），释放后欠阻尼产生弹性回弹
        float stiffness = _dragging ? 260f : 130f;
        float damping = _dragging ? 26f : 12f;
        float accel = (_targetProgress - _displayProgress) * stiffness - _progressVelocity * damping;
        _progressVelocity += accel * dt;
        _displayProgress += _progressVelocity * dt;
        _displayProgress = Math.Clamp(_displayProgress, 0f, 1f);
        if (MathF.Abs(_targetProgress - _displayProgress) < 0.0005f && MathF.Abs(_progressVelocity) < 0.0005f)
        {
            _displayProgress = _targetProgress;
            _progressVelocity = 0f;
        }
    }

    private void UpdateJitter(float dt)
    {
        float target = _dragging ? 0.05f : _config.JitterAmount;
        float rate = _dragging ? 25f : 6f;   // 拖拽快速提升，释放约 0.5s 衰减
        _jitter += (target - _jitter) * (1f - MathF.Exp(-rate * dt));
        _jitter = Math.Clamp(_jitter, 0.001f, 0.1f);
    }

    private void UpdateColors(float dt)
    {
        float t = 1f - MathF.Exp(-dt / MathF.Max(0.01f, _config.ColorTransitionSeconds));
        _currentGlow = FluidColor.Lerp(_currentGlow, _config.EffectiveGlowColor, t);
        _currentBackground = FluidColor.Lerp(_currentBackground, _config.EffectiveBackgroundColor, t);
        _currentAmbient = FluidColor.Lerp(_currentAmbient, _config.EffectiveAmbientColor, t);
    }

    private float UpdateCompletionPulse()
    {
        float pulse = 0f;
        if (_displayProgress >= 0.999f)
        {
            if (!_wasComplete)
            {
                _wasComplete = true;
                _completeTime = _elapsed;
            }
            float since = _elapsed - _completeTime;
            if (since < 1.2f)
            {
                pulse = MathF.Max(0f, MathF.Exp(-4f * since) * 0.6f * MathF.Sin(since * 22f));
            }
        }
        else if (_displayProgress < 0.97f)
        {
            _wasComplete = false;
        }
        return pulse;
    }

    // ==================== 绘制 ====================

    private void DrawCardBackground(SKCanvas canvas, SKRect rect, float intensity)
    {
        // 深色垂直渐变背景
        var top = _currentBackground.ToSKColor();
        var bottom = ScaleColor(top, 0.72f);
        using var bgPaint = new SKPaint { IsAntialias = true };
        bgPaint.Shader = SKShader.CreateLinearGradient(
            new SKPoint(0, rect.Top), new SKPoint(0, rect.Bottom),
            [top, bottom], [0f, 1f], SKShaderTileMode.Clamp);
        canvas.DrawRoundRect(rect, _clipRadius, _clipRadius, bgPaint);

        // 微弱噪点纹理
        if (_noisePaint != null)
        {
            canvas.DrawRect(rect, _noisePaint);
        }

        // 内描边：让卡片边缘有极微弱的质感
        using var edgePaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            Color = ScaleColor(_currentBackground.ToSKColor(), 1.6f).WithAlpha(40)
        };
        canvas.DrawRoundRect(rect, _clipRadius, _clipRadius, edgePaint);
    }

    private void DrawAmbientGlow(SKCanvas canvas, float baseX, float height, float intensity)
    {
        if (_config.AmbientGlow <= 0f)
            return;

        float glowWidth = MathF.Max(baseX + 0.3f * height, 0f);
        var ambient = _currentAmbient.ToSKColor();
        var a0 = ambient.WithAlpha((byte)(Math.Clamp(_config.AmbientGlow, 0f, 1f) * 255 * Math.Clamp(intensity, 0f, 2f)));
        var a1 = ambient.WithAlpha(0);
        using var paint = new SKPaint();
        paint.Shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0), new SKPoint(glowWidth, 0),
            [a0, a1], [0f, 1f], SKShaderTileMode.Clamp);
        canvas.DrawRect(new SKRect(0, 0, glowWidth, height), paint);
    }

    private void DrawTrail(SKCanvas canvas, float height, float intensity, float pulse)
    {
        var paint = _trailPaint!;
        paint.StrokeWidth = MathF.Max(1.5f, 0.02f * height);
        foreach (var frame in _trail)
        {
            if (frame.Life <= 0f || frame.Xs.Length < 2)
                continue;
            float alpha = frame.Life * 0.16f * intensity * (1f + pulse);
            paint.Color = _currentGlow.ToSKColor().WithAlpha((byte)(Math.Clamp(alpha, 0f, 1f) * 255));
            using var p = new SKPath();
            p.MoveTo(frame.Xs[0], 12f);
            for (int i = 1; i < frame.Xs.Length; i++)
                p.LineTo(frame.Xs[i], 12f + i * MathF.Max(3f, height / 72f));
            canvas.DrawPath(p, paint);
        }
    }

    private void DrawCometTail(SKCanvas canvas, float[] xs, float[] ys, float baseX, float amp, float intensity, float flicker, float dt)
    {
        // 彗星式光晕拖尾：闪电路径向左扫出的封闭多边形，水平渐变从闪电处最亮渐隐到透明，
        // 形状完全跟随闪电的锯齿扭曲；拖尾长度随移动速度平滑伸缩，
        // 且至少覆盖闪电最大摆动（2*amp），避免左凸锯齿导致多边形自交。
        float baseMove = float.IsNaN(_lastBaseX) ? 0f : MathF.Abs(baseX - _lastBaseX);
        _lastBaseX = baseX;
        _shadowSpeed += (baseMove - _shadowSpeed) * Math.Clamp(dt * 30f, 0f, 1f);
        float tailLen = MathF.Max(22f + Math.Clamp(_shadowSpeed * 3.5f, 0f, 70f), 2f * amp + 12f);

        int n = xs.Length;
        if (n < 2)
            return;

        using var tail = new SKPath();
        tail.MoveTo(xs[0], ys[0]);
        for (int i = 1; i < n; i++)
            tail.LineTo(xs[i], ys[i]);
        for (int i = n - 1; i >= 0; i--)
            tail.LineTo(xs[i] - tailLen, ys[i]);
        tail.Close();

        float maxAlpha = (0.14f + 0.10f * Math.Clamp(_shadowSpeed / 20f, 0f, 1f)) * intensity * flicker;
        var glow = _currentGlow.ToSKColor();
        var a0 = glow.WithAlpha(0);
        var aMid = glow.WithAlpha((byte)(Math.Clamp(maxAlpha * 0.35f, 0f, 1f) * 255));
        var a1 = glow.WithAlpha((byte)(Math.Clamp(maxAlpha, 0f, 1f) * 255));

        // 拖尾头部（贴近闪电处）较亮，向左快速衰减，营造彗星尾迹感；
        // 渐变右端覆盖到闪电右凸范围，避免 Clamp 造成不对称
        using var shader = SKShader.CreateLinearGradient(
            new SKPoint(baseX - tailLen, 0), new SKPoint(baseX + amp * 0.6f, 0),
            [a0, aMid, a1], [0f, 0.55f, 1f], SKShaderTileMode.Clamp);

        var paint = _shadowPaint!;
        paint.Shader = shader;
        canvas.DrawPath(tail, paint);
        paint.Shader = null;
    }

    private void DrawPathGlow(SKCanvas canvas, SKPath path, SKPaint outer, SKPaint mid)
    {
        // 外层大光晕
        canvas.DrawPath(path, outer);
        // 中层辉光
        canvas.DrawPath(path, mid);
    }

    private void DrawCorePath(SKCanvas canvas, SKPath path, SKPaint core)
    {
        canvas.DrawPath(path, core);
    }

    private void DrawForkGlow(SKCanvas canvas, List<(float X, float Y, float Length, float Lift)> forks, SKPaint paint)
    {
        if (forks.Count == 0)
            return;
        foreach (var (x, y, len, lift) in forks)
        {
            canvas.DrawLine(x, y + lift, x + len, y + lift * 0.6f, paint);
        }
    }

    private void DrawForks(SKCanvas canvas, List<(float X, float Y, float Length, float Lift)> forks, SKPaint paint)
    {
        if (forks.Count == 0)
            return;
        foreach (var (x, y, len, lift) in forks)
        {
            canvas.DrawLine(x, y + lift, x + len, y + lift * 0.6f, paint);
        }
    }

    private void DrawTexts(SKCanvas canvas, float width, float height, float intensity, float pulse)
    {
        float padX = 22f;
        var glow = _currentGlow.ToSKColor();

        if (_config.ShowTitle)
        {
            // 标题
            using var titlePaint = new SKPaint
            {
                IsAntialias = true,
                Color = LerpColor(SKColors.White, glow, 0.28f)
            };
            canvas.DrawText(_config.Title, padX, height * 0.44f, SKTextAlign.Left, _titleFont!, titlePaint);

            // 副标题
            if (!string.IsNullOrWhiteSpace(_config.Subtitle))
            {
                using var subPaint = new SKPaint
                {
                    IsAntialias = true,
                    Color = new SKColor(150, 158, 176, 220)
                };
                canvas.DrawText(_config.Subtitle, padX, height * 0.64f, SKTextAlign.Left, _subFont!, subPaint);
            }
        }

        if (_config.ShowPercentage)
        {
            // 百分比数字（右侧，弹性显示值）
            int value = (int)MathF.Round(_displayProgress * 100f);
            float alpha = Math.Clamp(0.9f + 0.1f * intensity + pulse, 0f, 1f);
            using var pctPaint = new SKPaint
            {
                IsAntialias = true,
                Color = LerpColor(glow, SKColors.White, 0.35f).WithAlpha((byte)(alpha * 255))
            };
            canvas.DrawText($"{value}%", width - padX, height * 0.72f, SKTextAlign.Right, _pctFont!, pctPaint);
        }
    }

    // ==================== 缓存资源 ====================

    private void EnsureCachedResources(int width, int height, float cornerRadius, float intensity)
    {
        EnsureClipPath(width, height, cornerRadius);
        EnsureNoise();
        EnsurePaintCache(height);
        EnsureFontCache(height);
    }

    private void EnsurePaintCache(float height)
    {
        if (MathF.Abs(_cacheHeight - height) < 0.01f)
            return;

        _cacheHeight = height;

        _outerBlur?.Dispose();
        _midBlur?.Dispose();
        _outerBlur = SKImageFilter.CreateBlur(0.14f * height, 0.14f * height);
        _midBlur = SKImageFilter.CreateBlur(0.05f * height, 0.05f * height);

        _outerPaint ??= CreateStrokePaint();
        _outerPaint.ImageFilter = _outerBlur;
        _outerPaint.StrokeWidth = MathF.Max(0.5f, 0.18f * height);

        _midPaint ??= CreateStrokePaint();
        _midPaint.ImageFilter = _midBlur;
        _midPaint.StrokeWidth = MathF.Max(0.5f, 0.07f * height);

        _corePaint ??= CreateStrokePaint();
        _corePaint.ImageFilter = null;
        _corePaint.StrokeWidth = MathF.Max(0.5f, 0.028f * height);

        _forkPaint ??= CreateStrokePaint();
        _forkPaint.ImageFilter = null;
        _forkPaint.StrokeWidth = MathF.Max(0.5f, 0.028f * height);

        _forkMidPaint ??= CreateStrokePaint();
        _forkMidPaint.ImageFilter = _midBlur;
        _forkMidPaint.StrokeWidth = MathF.Max(0.5f, 0.07f * height);

        _trailPaint ??= CreateStrokePaint();
        _trailPaint.ImageFilter = null;

        _shadowPaint ??= CreateStrokePaint();
        _shadowPaint.Style = SKPaintStyle.Fill;
        _shadowPaint.ImageFilter = null;
    }

    private static SKPaint CreateStrokePaint()
    {
        return new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round
        };
    }

    private void EnsureFontCache(float height)
    {
        if (_titleFont != null && MathF.Abs(_titleFont.Size - height * 0.24f) < 0.01f)
            return;

        _titleFont?.Dispose();
        _subFont?.Dispose();
        _pctFont?.Dispose();
        _titleFont = new SKFont(_boldTypeface, height * 0.24f) { Edging = SKFontEdging.Antialias };
        _subFont = new SKFont(_regularTypeface, height * 0.12f) { Edging = SKFontEdging.Antialias };
        _pctFont = new SKFont(_boldTypeface, height * 0.40f) { Edging = SKFontEdging.Antialias };
    }

    private void EnsureClipPath(int width, int height, float radius)
    {
        if (_clipPath != null && _clipRect.Width == width && _clipRect.Height == height && MathF.Abs(_clipRadius - radius) < 0.01f)
            return;

        _clipPath?.Dispose();
        _clipPath = new SKPath();
        _clipPath.AddRoundRect(new SKRect(0, 0, width, height), radius, radius);
        _clipRect = new SKRect(0, 0, width, height);
        _clipRadius = radius;
        // 尺寸变化时基线与位移重置，避免拖影突变
        _lastBaseX = float.NaN;
        _shadowSpeed = 0f;
    }

    private void EnsureNoise()
    {
        if (_noiseBitmap != null)
            return;

        const int tile = 96;
        _noiseBitmap = new SKBitmap(tile, tile, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        var rand = new Random(0xC0FFEE);
        unsafe
        {
            var ptr = (uint*)_noiseBitmap.GetPixels();
            for (int i = 0; i < tile * tile; i++)
            {
                byte g = (byte)rand.Next(0, 256);
                byte a = (byte)(g * 0.055f);
                // Unpremul：RGB 灰度与 alpha 分离存储
                ptr[i] = (uint)(0x00000000 | ((uint)a << 24) | ((uint)g << 16) | ((uint)g << 8) | g);
            }
        }

        _noisePaint ??= new SKPaint();
        _noisePaint.Shader = SKShader.CreateBitmap(_noiseBitmap, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);
    }

    private SKColor GlowAlpha(float alpha)
    {
        var c = _currentGlow.ToSKColor();
        return c.WithAlpha((byte)(Math.Clamp(alpha, 0f, 1f) * 255));
    }

    private SKColor CoreAlpha(float alpha)
    {
        // 核心线：白偏主题色
        return LerpColor(SKColors.White, _currentGlow.ToSKColor(), 0.55f)
            .WithAlpha((byte)(Math.Clamp(alpha, 0f, 1f) * 255));
    }

    private static SKColor LerpColor(SKColor a, SKColor b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new SKColor(
            (byte)(a.Red + (b.Red - a.Red) * t),
            (byte)(a.Green + (b.Green - a.Green) * t),
            (byte)(a.Blue + (b.Blue - a.Blue) * t),
            (byte)(a.Alpha + (b.Alpha - a.Alpha) * t));
    }

    private static SKColor ScaleColor(SKColor c, float factor)
    {
        return new SKColor(
            (byte)Math.Clamp(c.Red * factor, 0, 255),
            (byte)Math.Clamp(c.Green * factor, 0, 255),
            (byte)Math.Clamp(c.Blue * factor, 0, 255),
            c.Alpha);
    }

    private static float Noise1(float x)
    {
        int ix = (int)MathF.Floor(x);
        float fx = x - ix;
        fx = fx * fx * (3 - 2 * fx);
        return Hash1(ix) + (Hash1(ix + 1) - Hash1(ix)) * fx;
    }

    private static float Hash1(int n)
    {
        uint h = (uint)n;
        h = (h ^ 61) ^ (h >> 16);
        h += h << 3;
        h ^= h >> 4;
        h *= 0x27d4eb2d;
        h ^= h >> 15;
        return (h & 0x7fffffff) / (float)0x7fffffff;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _noisePaint?.Dispose();
        _noisePaint = null;
        _noiseBitmap?.Dispose();
        _noiseBitmap = null;
        _clipPath?.Dispose();
        _clipPath = null;
        _outerBlur?.Dispose();
        _outerBlur = null;
        _midBlur?.Dispose();
        _midBlur = null;
        _outerPaint?.Dispose();
        _outerPaint = null;
        _midPaint?.Dispose();
        _midPaint = null;
        _corePaint?.Dispose();
        _corePaint = null;
        _forkPaint?.Dispose();
        _forkPaint = null;
        _forkMidPaint?.Dispose();
        _forkMidPaint = null;
        _trailPaint?.Dispose();
        _trailPaint = null;
        _shadowPaint?.Dispose();
        _shadowPaint = null;
        _titleFont?.Dispose();
        _titleFont = null;
        _subFont?.Dispose();
        _subFont = null;
        _pctFont?.Dispose();
        _pctFont = null;
        _boldTypeface?.Dispose();
        _regularTypeface?.Dispose();
        GC.SuppressFinalize(this);
    }
}
