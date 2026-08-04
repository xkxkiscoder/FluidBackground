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
uniform float iSeed;
uniform float iMotion;
uniform float iAuroraProfile;
uniform float iStarScale;

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
    float size = mix(0.01, 0.035, r3) * iStarScale;   // 中等大小、边缘柔和
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

// 星云胶囊效果（来自nebula-capsules）
float hash21(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32 + iSeed);
    return fract(p.x * p.y);
}

float nebulaNoise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash21(i);
    float b = hash21(i + vec2(1.0, 0.0));
    float c = hash21(i + vec2(0.0, 1.0));
    float d = hash21(i + vec2(1.0, 1.0));
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float nebulaFbm(vec2 p) {
    float value = 0.0;
    float amplitude = 0.52;
    mat2 rotation = mat2(0.80, 0.60, -0.60, 0.80);
    for (int i = 0; i < 6; i++) {
        value += amplitude * nebulaNoise(p);
        p = rotation * p * 2.03 + 17.7;
        amplitude *= 0.5;
    }
    return value;
}

float gaussian(float value, float center, float width) {
    return exp(-pow(value - center, 2.0) / max(width, 0.0001));
}

vec3 nebulaPalette(float t) {
    t = clamp(t, 0.0, 1.0);
    vec3 shadow = mix(iColor0, iColor1, smoothstep(0.06, 0.62, t));
    vec3 body = mix(iColor1, iColor2, smoothstep(0.30, 0.82, t));
    vec3 highlight = mix(iColor2, iColor3, smoothstep(0.74, 1.0, t));
    vec3 restrained = mix(shadow, body, smoothstep(0.26, 0.72, t));
    return mix(restrained, highlight, smoothstep(0.78, 0.97, t));
}

vec3 renderNebula(vec2 uv, vec2 p, vec2 pointer, float distanceToPointer, float t) {
    vec2 delta = p - pointer;
    float influence = exp(-distanceToPointer * 4.6) * iMotion;
    float angle = influence * 1.7;
    mat2 swirl = mat2(cos(angle), -sin(angle), sin(angle), cos(angle));
    p = pointer + swirl * delta;
    p += normalize(delta + 0.0001) * influence * 0.08;

    vec2 drift = vec2(t * 0.22, -t * 0.13);
    vec2 q = vec2(
        nebulaFbm(p * 1.35 + drift + iSeed),
        nebulaFbm(p * 1.35 + vec2(5.2, 1.3) - drift * 0.85)
    );
    vec2 r = vec2(
        nebulaFbm(p * 2.0 + 3.6 * q + vec2(1.7, 9.2) + t * 0.10),
        nebulaFbm(p * 2.0 + 3.0 * q + vec2(8.3, 2.8) - t * 0.085)
    );

    float cloud = nebulaFbm(p * 1.7 + 4.2 * r);
    float veins = nebulaFbm(p * 4.0 - 2.0 * q + t * 0.065);
    float nebula = smoothstep(0.18, 0.91, cloud * 0.9 + veins * 0.22);

    vec3 color = nebulaPalette(nebula);
    color += iColor3 * pow(max(cloud - 0.63, 0.0), 2.0) * 1.05;
    color *= 0.78 + 0.34 * smoothstep(0.15, 0.9, veins);

    vec2 starGrid = floor((uv + vec2(iSeed * 0.013, 0.0)) * vec2(132.0, 58.0));
    vec2 starCell = fract(uv * vec2(132.0, 58.0)) - 0.5;
    float starRandom = hash21(starGrid);
    float starShape = smoothstep(0.075, 0.0, length(starCell));
    float starMask = step(0.989, starRandom) * starShape;
    float twinkle = 0.35 + 0.65 * sin(t * (1.0 + starRandom * 2.4) + starRandom * 40.0) * 0.5 + 0.5;
    color += starMask * twinkle * mix(iColor2, iColor3, starRandom) * 1.05;

    float pointerGlow = exp(-distanceToPointer * 7.0) * iMotion;
    color += iColor3 * pointerGlow * 0.28;
    return color;
}

// 极光效果（来自nebula-capsules）
vec3 renderPolar(vec2 uv, float distanceToPointer, float t) {
    float phase = t * 1.08 + iSeed * 0.063;
    float rightField = smoothstep(0.06, 0.96, uv.x);
    float grain = nebulaFbm(vec2(uv.x * 1.85 - phase * 0.14, uv.y * 2.45 + phase * 0.10) + iSeed) - 0.5;

    float orangeCenter = 0.76 - uv.x * 0.20 + sin(phase + uv.x * 3.7) * 0.14 + grain * 0.16;
    float magentaCenter = 0.37 + uv.x * 0.13 + sin(phase * 0.84 + uv.x * 4.7 + 1.1) * 0.16 - grain * 0.14;
    float lowerCenter = 0.13 + sin(phase * 0.72 + uv.x * 3.8) * 0.10;
    float sweepCenter = 0.54 + sin(phase * 1.22 + uv.x * 5.4) * 0.08;

    float orangeBand = gaussian(uv.y, orangeCenter, 0.074);
    float magentaBand = gaussian(uv.y, magentaCenter, 0.115);
    float lowerBand = gaussian(uv.y, lowerCenter, 0.070);
    float sweepBand = gaussian(uv.y, sweepCenter, 0.027) * smoothstep(0.28, 0.98, uv.x);

    vec2 corePosition = vec2(
        0.945 + sin(phase * 0.68) * 0.052,
        0.60 + cos(phase * 0.83) * 0.145
    );
    float whiteCore = exp(-length((uv - corePosition) * vec2(2.05, 0.94)) * 6.1);
    float secondaryCore = exp(-length((uv - vec2(0.90 + cos(phase * 0.47) * 0.055, 0.27 + sin(phase * 0.64) * 0.08)) * vec2(2.4, 1.15)) * 7.0);
    float pulse = 0.72 + sin(phase * 1.62) * 0.28;
    float pointerBend = exp(-distanceToPointer * 6.4) * iMotion;

    vec3 color = iColor0;
    color = mix(color, iColor1, clamp(orangeBand * rightField * 1.16, 0.0, 1.0));
    color = mix(color, iColor2, clamp((magentaBand * 1.18 + lowerBand * 0.82) * rightField, 0.0, 1.0));
    color += iColor3 * whiteCore * pulse * 1.30;
    color += mix(iColor3, iColor2, 0.30) * secondaryCore * 0.58;
    color += mix(iColor3, iColor2, 0.55) * sweepBand * 0.42;
    color += iColor2 * pointerBend * rightField * 0.24;
    color += mix(iColor1, iColor2, 0.55) * smoothstep(0.35, 0.94, grain + 0.5) * rightField * 0.18;
    return color;
}

vec3 renderDubdot(vec2 uv, float distanceToPointer, float t) {
    float phase = t * 0.86 + iSeed * 0.051;
    float rightField = smoothstep(0.16, 0.97, uv.x);
    float drift = nebulaFbm(vec2(uv.x * 1.25 - phase * 0.105, uv.y * 1.95 + phase * 0.075) + iSeed) - 0.5;

    float upperCenter = 0.72 - uv.x * 0.18 + sin(phase + uv.x * 3.4) * 0.115 + drift * 0.15;
    float lowerCenter = 0.28 + uv.x * 0.11 + cos(phase * 0.88 + uv.x * 3.2) * 0.125 - drift * 0.12;
    float middleCenter = 0.50 + sin(phase * 1.18 + uv.x * 4.8) * 0.075;
    float upperBand = gaussian(uv.y, upperCenter, 0.115);
    float lowerBand = gaussian(uv.y, lowerCenter, 0.125);
    float middleBand = gaussian(uv.y, middleCenter, 0.052) * smoothstep(0.34, 0.98, uv.x);
    float softBody = exp(-length((uv - vec2(0.87 + sin(phase * 0.52) * 0.065, 0.51 + cos(phase * 0.44) * 0.045)) * vec2(1.42, 0.72)) * 2.85);
    float pointerBend = exp(-distanceToPointer * 6.8) * iMotion;

    vec3 color = iColor0;
    color = mix(color, iColor1, clamp(softBody * rightField * 0.74, 0.0, 1.0));
    color = mix(color, iColor2, clamp((upperBand * 0.76 + middleBand * 0.28) * rightField, 0.0, 1.0));
    color = mix(color, iColor3, clamp((lowerBand * 0.82 + softBody * 0.46 + middleBand * 0.34) * rightField, 0.0, 1.0));
    color += mix(iColor2, iColor3, 0.58) * middleBand * rightField * 0.18;
    color = mix(color, vec3(1.0), smoothstep(0.0, 0.34, 1.0 - rightField) * 0.24);
    color += iColor3 * pointerBend * rightField * 0.15;
    return color;
}

vec3 renderVercel(vec2 uv, float distanceToPointer, float t) {
    float phase = t * 1.62 + iSeed * 0.044;
    float rightField = smoothstep(0.12, 0.97, uv.x);
    float flowNoise = nebulaFbm(vec2(uv.x * 1.28 - phase * 0.12, uv.y * 1.92 + phase * 0.09) + iSeed) - 0.5;

    float mintCenter = 0.78 - uv.x * 0.24 + sin(phase + uv.x * 3.9) * 0.16 + flowNoise * 0.13;
    float goldCenter = 0.50 + sin(phase * 0.86 + uv.x * 4.5 + 1.7) * 0.18 - flowNoise * 0.11;
    float pinkCenter = 0.20 + uv.x * 0.17 + sin(phase * 1.08 + uv.x * 3.6 + 3.0) * 0.15 + flowNoise * 0.10;

    float mintBand = gaussian(uv.y, mintCenter, 0.105);
    float goldBand = gaussian(uv.y, goldCenter, 0.115);
    float pinkBand = gaussian(uv.y, pinkCenter, 0.100);

    float mintCore = exp(-length((uv - vec2(
        0.88 + sin(phase * 0.68) * 0.085,
        0.74 + cos(phase * 0.82) * 0.13
    )) * vec2(1.48, 0.82)) * 3.35);

    float goldCore = exp(-length((uv - vec2(
        0.92 + cos(phase * 0.61 + 1.2) * 0.080,
        0.50 + sin(phase * 0.77) * 0.15
    )) * vec2(1.42, 0.80)) * 3.20);

    float pinkCore = exp(-length((uv - vec2(
        0.86 + sin(phase * 0.73 + 2.1) * 0.095,
        0.27 + cos(phase * 0.66) * 0.13
    )) * vec2(1.44, 0.82)) * 3.28);

    float rightBody = exp(-length((uv - vec2(
        0.91 + sin(phase * 0.38) * 0.045,
        0.50 + cos(phase * 0.42) * 0.055
    )) * vec2(1.20, 0.68)) * 2.62);

    float separation = gaussian(
        uv.y,
        0.49 + sin(phase * 0.94 + uv.x * 5.0) * 0.10,
        0.035
    ) * smoothstep(0.34, 0.98, uv.x);

    float pointerBend = exp(-distanceToPointer * 6.8) * iMotion;

    vec3 color = iColor0;
    color = mix(color, iColor1, clamp((mintBand * 0.86 + mintCore * 0.72 + rightBody * 0.18) * rightField, 0.0, 1.0));
    color = mix(color, iColor2, clamp((goldBand * 0.90 + goldCore * 0.76 + rightBody * 0.16) * rightField, 0.0, 1.0));
    color = mix(color, iColor3, clamp((pinkBand * 0.84 + pinkCore * 0.70 + rightBody * 0.12) * rightField, 0.0, 1.0));

    color += iColor1 * mintBand * rightField * 0.10;
    color += iColor2 * goldBand * rightField * 0.11;
    color += iColor3 * pinkBand * rightField * 0.10;
    color = mix(color, vec3(1.0), separation * 0.11);
    color += mix(iColor1, iColor3, 0.5) * pointerBend * rightField * 0.10;
    return color;
}

vec3 renderAuroraProfile(vec2 uv, float distanceToPointer, float t) {
    if (iAuroraProfile < 1.5) return renderPolar(uv, distanceToPointer, t);
    if (iAuroraProfile < 2.5) return renderDubdot(uv, distanceToPointer, t);
    return renderVercel(uv, distanceToPointer, t);
}

void main() {
    vec2 uv = TexCoord;
    float time = iTime * iSpeed;

    vec3 color;

    if (iMode < 0.5) {
        // 流体模式
        color = fluidEffect(uv, time);
    } else if (iMode < 1.5) {
        // 星空模式
        color = starfieldEffect(uv, time);
    } else if (iMode < 2.5) {
        // 星云胶囊模式
        vec2 p = uv - 0.5;
        p.x *= iResolution.x / max(iResolution.y, 1.0);
        vec2 pointer = iPointer - 0.5;
        pointer.x *= iResolution.x / max(iResolution.y, 1.0);
        float distanceToPointer = length(p - pointer);
        color = renderNebula(uv, p, pointer, distanceToPointer, time);
    } else {
        // 极光模式
        float distanceToPointer = length(uv - iPointer);
        color = renderAuroraProfile(uv, distanceToPointer, time);
    }

    float pInfluence = pointerInfluence(uv);
    color = mix(color, color * 1.5, pInfluence * 0.5);

    float vignette = 1.0 - smoothstep(0.4, 1.4, length(uv - 0.5) * 1.5);
    color *= vignette;

    color = clamp(color, 0.0, 1.0);

    FragColor = vec4(color, 1.0);
}
