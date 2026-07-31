#version 330 core

out vec4 FragColor;

in vec2 TexCoord;

uniform vec2 iResolution;
uniform float iTime;
uniform float iSpeed;
uniform float iIntensity;
uniform vec2 iPointer;
uniform float iPointerRadius;
uniform float iMode;
uniform float iEnablePointer;
uniform vec3 iColor0;
uniform vec3 iColor1;
uniform vec3 iColor2;
uniform vec3 iColor3;
uniform float iColorCount;

// 噪声函数
float hash(vec2 p) {
    float h = dot(p, vec2(127.1, 311.7));
    return fract(sin(h) * 43758.5453123);
}

float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);

    float a = hash(i);
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));

    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float fbm(vec2 p) {
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    for (int i = 0; i < 5; i++) {
        value += amplitude * noise(p * frequency);
        amplitude *= 0.5;
        frequency *= 2.0;
    }

    return value;
}

// 3D噪声（用于体积效果）
float noise3D(vec3 p) {
    vec3 i = floor(p);
    vec3 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);

    vec2 uv = (i.xy + vec2(37.0, 17.0) * i.z) + f.xy;
    float n = noise(uv);

    return n;
}

float fbm3D(vec3 p) {
    float value = 0.0;
    float amplitude = 0.5;

    for (int i = 0; i < 4; i++) {
        value += amplitude * noise3D(p);
        amplitude *= 0.5;
        p *= 2.0;
    }

    return value;
}

// 流体流动效果（带3D体积感）
vec3 fluidEffect(vec2 uv, float time) {
    vec2 q = vec2(fbm(uv + time * 0.1), fbm(uv + vec2(5.2, 1.3) + time * 0.12));
    vec2 r = vec2(fbm(uv + 4.0 * q + vec2(1.7, 9.2) + time * 0.15),
                  fbm(uv + 4.0 * q + vec2(8.3, 2.8) + time * 0.126));

    float f = fbm(uv + 4.0 * r);

    vec3 volume = vec3(fbm3D(vec3(uv * 2.0, time * 0.2)));
    f = mix(f, volume.x, 0.3);

    vec3 color = mix(iColor0, iColor1, clamp(f * f * 4.0, 0.0, 1.0));
    color = mix(color, iColor2, clamp(length(q), 0.0, 1.0));
    color = mix(color, iColor3, clamp(length(r.x), 0.0, 1.0));

    return color * (0.5 * f * f * f + 0.6 * f * f + 0.5 * f);
}

// 波纹效果（带3D深度）
vec3 rippleEffect(vec2 uv, float time) {
    vec2 center = vec2(0.5, 0.5);
    float dist = length(uv - center);

    float depth = fbm3D(vec3(uv * 3.0, time * 0.3));
    float wave = sin(dist * 20.0 - time * 3.0 + depth * 2.0) * 0.5 + 0.5;
    wave *= exp(-dist * 3.0);

    float angle = atan(uv.y - center.y, uv.x - center.x);
    float spiral = sin(angle * 3.0 + dist * 10.0 - time * 2.0 + depth) * 0.5 + 0.5;

    float pattern = mix(wave, spiral, 0.3) * iIntensity;

    vec3 color = mix(iColor0, iColor1, pattern);
    color = mix(color, iColor2, wave * 0.5);
    color = mix(color, iColor3, spiral * 0.3);

    return color;
}

// 呼吸效果（带体积发光）
vec3 breathingEffect(vec2 uv, float time) {
    float breath = sin(time * 0.5) * 0.5 + 0.5;
    vec2 center = vec2(0.5, 0.5);
    float dist = length(uv - center);

    float volume = fbm3D(vec3(uv * 2.0, time * 0.1));
    float pulse = smoothstep(breath + 0.2, breath - 0.2, dist + volume * 0.2);
    float glow = exp(-dist * 2.0) * breath;

    float n = fbm(uv * 3.0 + time * 0.1);
    float pattern = mix(pulse, n, 0.3) * iIntensity;

    vec3 color = mix(iColor0, iColor1, pattern);
    color = mix(color, iColor2, glow);
    color += iColor3 * glow * 0.3;

    return color;
}

// 指针交互
float pointerInfluence(vec2 uv) {
    if (iEnablePointer < 0.5) return 0.0;

    float dist = length(uv - iPointer);
    return smoothstep(iPointerRadius, 0.0, dist);
}

void main() {
    vec2 uv = TexCoord;
    float time = iTime * iSpeed;

    vec3 color;

    if (iMode < 0.5) {
        color = fluidEffect(uv, time);
    } else if (iMode < 1.5) {
        color = rippleEffect(uv, time);
    } else {
        color = breathingEffect(uv, time);
    }

    float pInfluence = pointerInfluence(uv);
    color = mix(color, color * 1.5, pInfluence * 0.5);

    float vignette = 1.0 - smoothstep(0.4, 1.4, length(uv - 0.5) * 1.5);
    color *= vignette;

    color = clamp(color, 0.0, 1.0);

    FragColor = vec4(color, 1.0);
}
