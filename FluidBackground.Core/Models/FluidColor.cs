using SkiaSharp;

namespace FluidBackground.Core.Models;

/// <summary>
/// 流体颜色定义，使用归一化RGB值（0.0-1.0）
/// </summary>
public readonly struct FluidColor : IEquatable<FluidColor>
{
    /// <summary>
    /// 红色分量（0.0-1.0）
    /// </summary>
    public float R { get; }

    /// <summary>
    /// 绿色分量（0.0-1.0）
    /// </summary>
    public float G { get; }

    /// <summary>
    /// 蓝色分量（0.0-1.0）
    /// </summary>
    public float B { get; }

    /// <summary>
    /// 创建流体颜色
    /// </summary>
    /// <param name="r">红色分量（0.0-1.0）</param>
    /// <param name="g">绿色分量（0.0-1.0）</param>
    /// <param name="b">蓝色分量（0.0-1.0）</param>
    public FluidColor(float r, float g, float b)
    {
        R = Math.Clamp(r, 0f, 1f);
        G = Math.Clamp(g, 0f, 1f);
        B = Math.Clamp(b, 0f, 1f);
    }

    /// <summary>
    /// 从字节值创建颜色（0-255）
    /// </summary>
    public static FluidColor FromBytes(byte r, byte g, byte b) =>
        new(r / 255f, g / 255f, b / 255f);

    /// <summary>
    /// 从十六进制字符串创建颜色（如 "#FF0000" 或 "FF0000"）
    /// </summary>
    public static FluidColor FromHex(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length != 6)
            throw new ArgumentException("十六进制颜色必须为6位", nameof(hex));

        var r = Convert.ToByte(hex[..2], 16);
        var g = Convert.ToByte(hex[2..4], 16);
        var b = Convert.ToByte(hex[4..6], 16);
        return FromBytes(r, g, b);
    }

    /// <summary>
    /// 从SKColor转换
    /// </summary>
    public static FluidColor FromSKColor(SKColor color) =>
        new(color.Red / 255f, color.Green / 255f, color.Blue / 255f);

    /// <summary>
    /// 转换为SKColor
    /// </summary>
    public SKColor ToSKColor() =>
        new((byte)(R * 255), (byte)(G * 255), (byte)(B * 255));

    /// <summary>
    /// 转换为归一化浮点数组 [R, G, B]
    /// </summary>
    public float[] ToArray() => [R, G, B];

    /// <summary>
    /// 线性插值
    /// </summary>
    public static FluidColor Lerp(FluidColor a, FluidColor b, float t) =>
        new(
            a.R + (b.R - a.R) * t,
            a.G + (b.G - a.G) * t,
            a.B + (b.B - a.B) * t
        );

    public bool Equals(FluidColor other) =>
        Math.Abs(R - other.R) < 0.001f &&
        Math.Abs(G - other.G) < 0.001f &&
        Math.Abs(B - other.B) < 0.001f;

    public override bool Equals(object? obj) =>
        obj is FluidColor other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(R, G, B);

    public static bool operator ==(FluidColor left, FluidColor right) =>
        left.Equals(right);

    public static bool operator !=(FluidColor left, FluidColor right) =>
        !left.Equals(right);

    public override string ToString() =>
        $"#{(byte)(R * 255):X2}{(byte)(G * 255):X2}{(byte)(B * 255):X2}";
}
