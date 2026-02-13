shader_type canvas_item;

uniform sampler2D fog_texture : filter_nearest;
uniform vec2 fog_offset;
uniform float fog_scale = 16.0;
uniform float visibility_threshold = 1.0;

void fragment() {
    // Get world position of this pixel
    vec2 world_pos = (FRAGCOORD.xy * SCREEN_PIXEL_SIZE * vec2(textureSize(TEXTURE, 0))) + fog_offset;
    
    // Convert to fog texture coordinates
    vec2 fog_uv = (world_pos - fog_offset) / fog_scale / vec2(textureSize(fog_texture, 0));
    
    // Sample fog texture
    float fog_alpha = texture(fog_texture, fog_uv).a;
    
    // If fog is too dark (high alpha), hide this sprite
    if (fog_alpha > visibility_threshold) {
        discard;
    }
    
    // Otherwise render normally
    COLOR = texture(TEXTURE, UV);
}
