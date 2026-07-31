#version 330 core

out vec4 FragColor;

in vec2 TexCoord;

uniform vec2 iResolution;
uniform float iTime;
uniform float iSpeed;
uniform float iDensity;
uniform float iMode;
uniform vec2 iPointer;
uniform float iPointerRadius;
uniform float iEnablePointer;
uniform float iEnableMeteor;
uniform float iEnableNebula;
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
    vec2 p = uv;
    vec2 q = vec2(fbm(p + time * 0.1), fbm(p + vec2(5.2, 1.3) + time * 0.12));
    vec2 r = vec2(fbm(p + 4.0 * q + vec2(1.7, 9.2) + time * 0.15),
                  fbm(p + 4.0 * q + vec2(8.3, 2.8) + time * 0.126));

    float f = fbm(p + 4.0 * r);

    vec3 volume = vec3(fbm3D(vec3(uv * 2.0, time * 0.2)));
    f = mix(f, volume.x, 0.3);

    vec3 color = mix(iColor0, iColor1, clamp(f * f * 4.0, 0.0, 1.0));
    color = mix(color, iColor2, clamp(length(q), 0.0, 1.0));
    color = mix(color, iColor3, clamp(length(r.x), 0.0, 1.0));

    // 浓度：低 → 色彩饱和度降低（向平均色收敛，但保持原亮度，避免整体变亮/变暗）
    vec3 midColor = (iColor0 + iColor1 + iColor2 + iColor3) * 0.25;
    vec3 lowSat = mix(midColor, color, 0.35 + 0.65 * iDensity);
    float lum = dot(color, vec3(0.2126, 0.7152, 0.0722));
    float lowSatLum = dot(lowSat, vec3(0.2126, 0.7152, 0.0722));
    color = lowSat * (lum / max(lowSatLum, 1e-4));

    return color * (0.5 * f * f * f + 0.6 * f * f + 0.5 * f);
}

// 星点层（density 控制出现概率，低浓度星点稀疏但大小不变）
float starLayer(vec2 uv, float scale, float time, float density) {
    vec2 p = uv * scale;
    vec2 cell = floor(p);
    vec2 f = fract(p);

    float r0 = hash(cell + vec2(3.0, 3.0));
    if (r0 > 0.9 * density) return 0.0;   // 浓度越高出星越多

    float r1 = hash(cell);
    float r2 = hash(cell + vec2(1.0, 0.0));
    float r3 = hash(cell + vec2(0.0, 1.0));
    float r4 = hash(cell + vec2(1.0, 1.0));
    float r5 = hash(cell + vec2(2.0, 2.0));

    float dist = length(f - vec2(r1, r2));
    float size = mix(0.01, 0.035, r3);   // 中等大小、边缘柔和
    float brightness = mix(0.3, 0.9, r4);
    float twinkle = 0.5 + 0.5 * sin(time * (1.5 + r5 * 2.5) + r5 * 6.283);

    return smoothstep(size, 0.0, dist) * brightness * (0.4 + 0.6 * twinkle);
}

// 流星
float meteor(vec2 uv, float time) {
    float cycle = 12.0;
    float seed = floor(time / cycle);
    float phase = fract(time / cycle);

    // 每颗流星随机存在时长（1.5 ~ 3.5 秒）
    float durFrac = mix(1.5, 3.5, hash(vec2(seed, 2.0))) / cycle;
    if (phase > durFrac) return 0.0;

    vec2 start = vec2(hash(vec2(seed, 0.0)) * 0.7, hash(vec2(seed, 1.0)) * 0.4);
    vec2 dir = normalize(vec2(0.8, 1.0)); // 左上 → 右下

    float life = phase / durFrac;
    vec2 head = start + dir * life * 1.2;
    vec2 toUv = uv - head;
    float proj = dot(toUv, -dir);
    float distToLine = length(toUv + dir * clamp(proj, 0.0, 0.35));

    float tail = exp(-distToLine * 140.0) * exp(-proj * 8.0) * 0.8;
    float headGlow = exp(-length(toUv) * 240.0) * 0.9;

    float visible = smoothstep(0.0, 0.15, life) * (1.0 - smoothstep(0.7, 1.0, life));
    return (tail + headGlow) * visible;
}

// 彩色星云
vec3 nebula(vec2 uv, float time) {
    vec2 q = vec2(fbm(uv * 2.0 + time * 0.03), fbm(uv * 2.0 + vec2(5.2, 1.3) + time * 0.02));
    vec3 n = vec3(
        fbm(uv * 2.0 + 3.0 * q + time * 0.015),
        fbm(uv * 2.0 + 3.0 * q + vec2(1.7, 9.2) + time * 0.018),
        fbm(uv * 2.0 + 3.0 * q + vec2(8.3, 2.8) + time * 0.021));

    vec3 color = mix(iColor0, iColor1, n.x);
    color = mix(color, iColor2, n.y);
    color = mix(color, iColor3, n.z);
    return color;
}

// 星空效果（闪烁星点、星云、流星）
vec3 starfieldEffect(vec2 uv, float time) {
    float density = iDensity;

    // 深空背景（径向渐暗，更深的夜空基调）
    float radial = length(uv - 0.5) * 1.6;
    vec3 color = mix(vec3(0.03, 0.04, 0.10), vec3(0.0, 0.0, 0.0), smoothstep(0.2, 1.2, radial));

    // 彩色星云（频率固定，浓度控制浓郁程度，低浓度时近乎深空）
    if (iEnableNebula > 0.5) {
        color += nebula(uv, time) * (0.25 * density);
    }

    // 星点（两层，网格固定，浓度控制出现概率；亮度恒定）
    vec2 drift = vec2(time * 0.008, time * 0.005);
    vec2 suv = uv - drift;
    color += vec3(1.0) * starLayer(suv, 12.0, time, density) * 0.9;
    color += vec3(0.8, 0.85, 1.0) * starLayer(suv * 1.7, 20.0, time, density) * 0.6;

    // 流星
    if (iEnableMeteor > 0.5) {
        color += vec3(1.0) * meteor(uv, time);
    }

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
    } else {
        color = starfieldEffect(uv, time);
    }

    float pInfluence = pointerInfluence(uv);
    color = mix(color, color * 1.5, pInfluence * 0.5);

    float vignette = 1.0 - smoothstep(0.4, 1.4, length(uv - 0.5) * 1.5);
    color *= vignette;

    color = clamp(color, 0.0, 1.0);

    FragColor = vec4(color, 1.0);
}
