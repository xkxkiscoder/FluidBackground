using FluidBackground.Core.Models;
using Silk.NET.OpenGL;
using SkiaSharp;
using System.Runtime.InteropServices;

namespace FluidBackground.Core.Renderers;

/// <summary>
/// 基于OpenGL的3D流体渲染器
/// </summary>
public class OpenGLRenderer : IFluidRenderer
{
    private GL? _gl;
    private uint _program;
    private uint _vao;
    private uint _vbo;
    private uint _fbo;
    private uint _fboTexture;
    private FluidConfig? _config;
    private float _pointerX = 0.5f;
    private float _pointerY = 0.5f;
    private bool _initialized;
    private int _lastWidth;
    private int _lastHeight;
    private bool _disposed;

    /// <inheritdoc/>
    public string Name => "OpenGL 3D";

    /// <inheritdoc/>
    public bool IsAvailable
    {
        get
        {
            try
            {
                return _gl != null && _initialized;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// 创建OpenGL渲染器（需要外部提供GL上下文）
    /// </summary>
    public OpenGLRenderer()
    {
    }

    /// <summary>
    /// 使用外部提供的GL上下文创建渲染器
    /// </summary>
    public OpenGLRenderer(GL gl)
    {
        _gl = gl;
    }

    /// <inheritdoc/>
    public bool Initialize(FluidConfig config)
    {
        _config = config.Clone();

        if (_gl == null)
        {
            Console.WriteLine("OpenGL上下文未提供，3D渲染器不可用");
            return false;
        }

        try
        {
            InitializeOpenGL();
            _initialized = true;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"初始化OpenGLRenderer失败: {ex.Message}");
            return false;
        }
    }

    private void InitializeOpenGL()
    {
        var vertexShaderSource = @"
#version 330 core
layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 aTexCoord;
out vec2 TexCoord;
void main()
{
    gl_Position = vec4(aPos, 0.0, 1.0);
    TexCoord = aTexCoord;
}";

        var fragmentShaderSource = LoadEmbeddedShader("FluidBackground.Core.Shaders.fluid.frag");

        var vertexShader = CompileShader(ShaderType.VertexShader, vertexShaderSource);
        var fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentShaderSource);

        _program = _gl!.CreateProgram();
        _gl.AttachShader(_program, vertexShader);
        _gl.AttachShader(_program, fragmentShader);
        _gl.LinkProgram(_program);

        _gl.GetProgram(_program, GLEnum.LinkStatus, out var status);
        if (status == 0)
        {
            var infoLog = _gl.GetProgramInfoLog(_program);
            throw new Exception($"着色器程序链接失败: {infoLog}");
        }

        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);

        float[] vertices =
        [
            -1.0f,  1.0f,  0.0f, 1.0f,
            -1.0f, -1.0f,  0.0f, 0.0f,
             1.0f, -1.0f,  1.0f, 0.0f,
            -1.0f,  1.0f,  0.0f, 1.0f,
             1.0f, -1.0f,  1.0f, 0.0f,
             1.0f,  1.0f,  1.0f, 1.0f
        ];

        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();

        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(GLEnum.ArrayBuffer, _vbo);

        unsafe
        {
            fixed (float* v = vertices)
            {
                _gl.BufferData(GLEnum.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), v, GLEnum.StaticDraw);
            }

            _gl.VertexAttribPointer(0, 2, GLEnum.Float, false, 4 * sizeof(float), (void*)0);
            _gl.EnableVertexAttribArray(0);

            _gl.VertexAttribPointer(1, 2, GLEnum.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
            _gl.EnableVertexAttribArray(1);
        }

        _gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        _gl.BindVertexArray(0);
    }

    private uint CompileShader(ShaderType type, string source)
    {
        var shader = _gl!.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        _gl.GetShader(shader, GLEnum.CompileStatus, out var status);
        if (status == 0)
        {
            var infoLog = _gl.GetShaderInfoLog(shader);
            throw new Exception($"{type}编译失败: {infoLog}");
        }

        return shader;
    }

    private void EnsureFramebuffer(int width, int height)
    {
        if (_fbo != 0 && _lastWidth == width && _lastHeight == height)
            return;

        CleanupFramebuffer();

        _fboTexture = _gl!.GenTexture();
        _gl.BindTexture(GLEnum.Texture2D, _fboTexture);
        unsafe
        {
            _gl.TexImage2D(GLEnum.Texture2D, 0, (int)GLEnum.Rgba, (uint)width, (uint)height, 0, GLEnum.Rgba, GLEnum.UnsignedByte, null);
        }
        _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
        _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);

        _fbo = _gl.GenFramebuffer();
        _gl.BindFramebuffer(GLEnum.Framebuffer, _fbo);
        _gl.FramebufferTexture2D(GLEnum.Framebuffer, GLEnum.ColorAttachment0, GLEnum.Texture2D, _fboTexture, 0);

        _gl.BindFramebuffer(GLEnum.Framebuffer, 0);
        _lastWidth = width;
        _lastHeight = height;
    }

    private void CleanupFramebuffer()
    {
        if (_fbo != 0)
        {
            _gl!.DeleteFramebuffer(_fbo);
            _fbo = 0;
        }
        if (_fboTexture != 0)
        {
            _gl!.DeleteTexture(_fboTexture);
            _fboTexture = 0;
        }
    }

    /// <inheritdoc/>
    public byte[] RenderFrame(double timeSeconds, int width, int height)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("渲染器未初始化或不可用");

        EnsureFramebuffer(width, height);

        _gl!.BindFramebuffer(GLEnum.Framebuffer, _fbo);
        _gl.Viewport(0, 0, (uint)width, (uint)height);

        RenderScene(timeSeconds, width, height);

        var pixels = new byte[width * height * 4];
        unsafe
        {
            fixed (byte* p = pixels)
            {
                _gl.ReadPixels(0, 0, (uint)width, (uint)height, GLEnum.Rgba, GLEnum.UnsignedByte, p);
            }
        }

        _gl.BindFramebuffer(GLEnum.Framebuffer, 0);

        FlipImageVertically(pixels, width, height);

        return pixels;
    }

    /// <inheritdoc/>
    public SKBitmap RenderToBitmap(double timeSeconds, int width, int height)
    {
        var pixels = RenderFrame(timeSeconds, width, height);
        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);

        Marshal.Copy(pixels, 0, bitmap.GetPixels(), pixels.Length);
        return bitmap;
    }

    /// <inheritdoc/>
    public void RenderToBitmap(double timeSeconds, int width, int height, SKBitmap target)
    {
        var pixels = RenderFrame(timeSeconds, width, height);
        Marshal.Copy(pixels, 0, target.GetPixels(), pixels.Length);
    }

    private void RenderScene(double timeSeconds, int width, int height)
    {
        _gl!.Clear(ClearBufferMask.ColorBufferBit);

        _gl.UseProgram(_program);

        SetUniform("iResolution", width, height);
        SetUniform("iTime", (float)timeSeconds);
        SetUniform("iSpeed", _config!.Speed);
        SetUniform("iDensity", _config.Density);
        SetUniform("iMode", (float)_config.Mode);
        SetUniform("iPointer", _pointerX, _pointerY);
        SetUniform("iPointerRadius", _config.PointerRadius);
        SetUniform("iEnablePointer", _config.EnablePointerInteraction ? 1.0f : 0.0f);
        SetUniform("iEnableMeteor", _config.EnableMeteor ? 1.0f : 0.0f);
        SetUniform("iEnableNebula", _config.EnableNebula ? 1.0f : 0.0f);
        SetUniform("iSeed", _config.Seed);
        SetUniform("iMotion", 0.5f);
        SetUniform("iAuroraProfile", (float)_config.AuroraProfile);
        SetUniform("iStarScale", _config.StarScale);

        var colors = _config.Colors;
        SetUniform("iColor0", colors[0].R, colors[0].G, colors[0].B);
        SetUniform("iColor1", colors.Length > 1 ? colors[1].R : colors[0].R,
                           colors.Length > 1 ? colors[1].G : colors[0].G,
                           colors.Length > 1 ? colors[1].B : colors[0].B);
        SetUniform("iColor2", colors.Length > 2 ? colors[2].R : colors[0].R,
                           colors.Length > 2 ? colors[2].G : colors[0].G,
                           colors.Length > 2 ? colors[2].B : colors[0].B);
        SetUniform("iColor3", colors.Length > 3 ? colors[3].R : colors[0].R,
                           colors.Length > 3 ? colors[3].G : colors[0].G,
                           colors.Length > 3 ? colors[3].B : colors[0].B);
        SetUniform("iColorCount", (float)colors.Length);

        _gl.BindVertexArray(_vao);
        _gl.DrawArrays(GLEnum.Triangles, 0, 6);
        _gl.BindVertexArray(0);
    }

    private void SetUniform(string name, float value)
    {
        var location = _gl!.GetUniformLocation(_program, name);
        if (location >= 0)
            _gl.Uniform1(location, value);
    }

    private void SetUniform(string name, float x, float y)
    {
        var location = _gl!.GetUniformLocation(_program, name);
        if (location >= 0)
            _gl.Uniform2(location, x, y);
    }

    private void SetUniform(string name, float x, float y, float z)
    {
        var location = _gl!.GetUniformLocation(_program, name);
        if (location >= 0)
            _gl.Uniform3(location, x, y, z);
    }

    private static void FlipImageVertically(byte[] pixels, int width, int height)
    {
        var stride = width * 4;
        var temp = new byte[stride];

        for (int y = 0; y < height / 2; y++)
        {
            var topOffset = y * stride;
            var bottomOffset = (height - 1 - y) * stride;

            Array.Copy(pixels, topOffset, temp, 0, stride);
            Array.Copy(pixels, bottomOffset, pixels, topOffset, stride);
            Array.Copy(temp, 0, pixels, bottomOffset, stride);
        }
    }

    /// <inheritdoc/>
    public void UpdateConfig(FluidConfig config)
    {
        _config = config.Clone();
    }

    /// <inheritdoc/>
    public void SetPointerPosition(float x, float y)
    {
        _pointerX = Math.Clamp(x, 0f, 1f);
        _pointerY = Math.Clamp(y, 0f, 1f);
    }

    private static string LoadEmbeddedShader(string resourceName)
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"找不到嵌入资源: {resourceName}");
        using var reader = new System.IO.StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            CleanupFramebuffer();

            if (_vbo != 0)
            {
                _gl!.DeleteBuffer(_vbo);
                _vbo = 0;
            }

            if (_vao != 0)
            {
                _gl!.DeleteVertexArray(_vao);
                _vao = 0;
            }

            if (_program != 0)
            {
                _gl!.DeleteProgram(_program);
                _program = 0;
            }

            _initialized = false;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
