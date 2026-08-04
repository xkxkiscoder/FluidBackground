using SkiaSharp;

namespace FluidBackground.Core.Models;

/// <summary>
/// 流体颜色定义，使用归一化RGB值（0.0-1.0）
/// <example>
/// <code>
/// // 创建颜色的多种方式
/// var red = new FluidColor(1.0f, 0.0f, 0.0f);           // RGB浮点值
/// var blue = FluidColor.FromHex("#0077B6");              // 十六进制字符串
/// var green = FluidColor.FromBytes(0, 255, 0);           // 字节值（0-255）
/// var yellow = FluidColor.FromSKColor(SKColors.Yellow);  // 从SKColor转换
/// </code>
/// </example>
/// </summary>
public readonly struct FluidColor : IEquatable<FluidColor>
{
    /// <summary>
    /// 红色分量（0.0-1.0）
    /// <para>0.0表示无红色，1.0表示最大红色强度</para>
    /// </summary>
    public float R { get; }

    /// <summary>
    /// 绿色分量（0.0-1.0）
    /// <para>0.0表示无绿色，1.0表示最大绿色强度</para>
    /// </summary>
    public float G { get; }

    /// <summary>
    /// 蓝色分量（0.0-1.0）
    /// <para>0.0表示无蓝色，1.0表示最大蓝色强度</para>
    /// </summary>
    public float B { get; }

    /// <summary>
    /// 创建流体颜色
    /// <para>使用归一化RGB值（0.0-1.0）创建颜色，超出范围的值会被自动限制</para>
    /// </summary>
    /// <param name="r">红色分量（0.0-1.0）</param>
    /// <param name="g">绿色分量（0.0-1.0）</param>
    /// <param name="b">蓝色分量（0.0-1.0）</param>
    /// <example>
    /// <code>
    /// var color = new FluidColor(0.5f, 0.8f, 0.2f);  // 中等强度的绿色
    /// </code>
    /// </example>
    public FluidColor(float r, float g, float b)
    {
        R = Math.Clamp(r, 0f, 1f);
        G = Math.Clamp(g, 0f, 1f);
        B = Math.Clamp(b, 0f, 1f);
    }

    /// <summary>
    /// 从字节值创建颜色（0-255）
    /// <para>将0-255范围的字节值转换为0.0-1.0范围的浮点值</para>
    /// </summary>
    /// <param name="r">红色分量（0-255）</param>
    /// <param name="g">绿色分量（0-255）</param>
    /// <param name="b">蓝色分量（0-255）</param>
    /// <returns>流体颜色实例</returns>
    /// <example>
    /// <code>
    /// var red = FluidColor.FromBytes(255, 0, 0);      // 纯红色
    /// var white = FluidColor.FromBytes(255, 255, 255); // 纯白色
    /// </code>
    /// </example>
    public static FluidColor FromBytes(byte r, byte g, byte b) =>
        new(r / 255f, g / 255f, b / 255f);

    /// <summary>
    /// 从十六进制字符串创建颜色
    /// <para>支持带或不带#号的6位十六进制颜色代码</para>
    /// </summary>
    /// <param name="hex">十六进制颜色字符串（如 "#FF0000"、"FF0000"、"#ff0000"）</param>
    /// <returns>流体颜色实例</returns>
    /// <exception cref="ArgumentException">当十六进制字符串格式不正确时抛出</exception>
    /// <example>
    /// <code>
    /// var red = FluidColor.FromHex("#FF0000");     // 纯红色
    /// var blue = FluidColor.FromHex("0077B6");     // 蓝色（不带#号）
    /// var green = FluidColor.FromHex("#10B981");   // 绿色
    /// </code>
    /// </example>
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
    /// <para>将SkiaSharp的SKColor类型转换为FluidColor</para>
    /// </summary>
    /// <param name="color">SKColor实例</param>
    /// <returns>流体颜色实例</returns>
    public static FluidColor FromSKColor(SKColor color) =>
        new(color.Red / 255f, color.Green / 255f, color.Blue / 255f);

    /// <summary>
    /// 转换为SKColor
    /// <para>将FluidColor转换为SkiaSharp的SKColor类型</para>
    /// </summary>
    /// <returns>SKColor实例</returns>
    public SKColor ToSKColor() =>
        new((byte)(R * 255), (byte)(G * 255), (byte)(B * 255));

    /// <summary>
    /// 转换为归一化浮点数组 [R, G, B]
    /// <para>返回长度为3的数组，值范围0.0-1.0</para>
    /// </summary>
    /// <returns>RGB浮点数组</returns>
    public float[] ToArray() => [R, G, B];

    /// <summary>
    /// 线性插值
    /// <para>在两个颜色之间进行平滑过渡</para>
    /// </summary>
    /// <param name="a">起始颜色</param>
    /// <param name="b">目标颜色</param>
    /// <param name="t">插值参数（0.0返回颜色a，1.0返回颜色b）</param>
    /// <returns>插值后的颜色</returns>
    /// <example>
    /// <code>
    /// var red = new FluidColor(1, 0, 0);
    /// var blue = new FluidColor(0, 0, 1);
    /// var purple = FluidColor.Lerp(red, blue, 0.5f); // 紫色
    /// </code>
    /// </example>
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

    /// <summary>
    /// 转换为十六进制字符串表示
    /// </summary>
    /// <returns>十六进制颜色字符串（如 "#FF0000"）</returns>
    public override string ToString() =>
        $"#{(byte)(R * 255):X2}{(byte)(G * 255):X2}{(byte)(B * 255):X2}";
}
