shader_type canvas_item;

uniform sampler2D fog_texture;
uniform vec2 fog_offset;
uniform float fog_scale = 16.0;

varying vec2 world_position;

void vertex() {
    // Calculate world position in vertex shader (once per vertex instead of per pixel)
    world_position = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

void fragment() {
    // Convert world position to fog texture coordinates
    vec2 fog_uv = (world_position - fog_offset) / fog_scale;
    vec2 fog_tex_size = vec2(textureSize(fog_texture, 0));
    fog_uv /= fog_tex_size;
    
    // Sample the fog texture
    vec4 fog_sample = texture(fog_texture, fog_uv);
    
    // Get the original sprite color
    vec4 sprite_color = texture(TEXTURE, UV);
    
    // If fog alpha is high (unrevealed), make enemy invisible
    if (fog_sample.a > 0.5) {
        sprite_color.a = 0.0;
    }
    
    COLOR = sprite_color;
}