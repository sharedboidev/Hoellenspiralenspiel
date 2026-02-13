shader_type canvas_item;

uniform sampler2D fog_texture : filter_nearest;
uniform vec2 fog_offset;
uniform float fog_scale = 16.0;
uniform vec2 sprite_global_position;
uniform float visibility_threshold = 0.5;
uniform bool debug_mode = true; // Set to true to see fog values

void fragment() {
    vec2 world_pos = sprite_global_position;
    vec2 fog_pixel_pos = (world_pos - fog_offset) / fog_scale;
    vec2 fog_tex_size = vec2(textureSize(fog_texture, 0));
    vec2 fog_uv = fog_pixel_pos / fog_tex_size;
    fog_uv = clamp(fog_uv, vec2(0.0), vec2(1.0));
    
    vec4 fog_color = texture(fog_texture, fog_uv);
    
    // DEBUG MODE: Show fog alpha as color
    if (debug_mode) {
        // Red channel = fog alpha
        // Green = whether it passes threshold (green = visible, black = hidden)
        float passes = fog_color.a <= visibility_threshold ? 1.0 : 0.0;
        COLOR = vec4(fog_color.a, passes, 0.0, 1.0);
    } else {
        // Normal rendering
        if (fog_color.a > visibility_threshold) {
            discard;
        }
        COLOR = texture(TEXTURE, UV);
    }
}
