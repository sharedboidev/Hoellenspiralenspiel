shader_type canvas_item;

uniform sampler2D noise: repeat_enable;
uniform vec2 drift_speed = vec2(0.02, 0.01);
uniform float min_alpha = 0.8; 

void fragment() {
    vec4 t = texture(TEXTURE, UV);
    
    vec2 animated_uv = UV + TIME * drift_speed;
    
    vec4 n = texture(noise, animated_uv);
    
    t.a -= t.r;
    t.rgb = n.rgb;
    t.a = min(t.a, min_alpha);
    COLOR = t;
}