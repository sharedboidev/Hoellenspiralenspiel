shader_type canvas_item;

uniform sampler2D noise: repeat_enable;
uniform vec2 drift_speed = vec2(0.002, 0.001);
uniform float fog_alpha = 0.95; 

void fragment() {
    vec4 t = texture(TEXTURE, UV);
    
    // Continuous drift to top right
    vec2 animated_uv = UV + (TIME/25.0) * drift_speed;
    vec4 n = texture(noise, animated_uv);
    
    t.a -= t.r;
    t.rgb = n.rgb;
    
    // If it's fogged (high alpha), set to fog_alpha
    // If it's revealed (low alpha), keep it transparent
    if (t.a > 0.5) {
        t.a = fog_alpha;
    }
    
    COLOR = t;
}