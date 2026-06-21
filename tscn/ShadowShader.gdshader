shader_type canvas_item;

uniform sampler2D block_data : filter_nearest;
uniform vec2 chunk_size = vec2(50.0, 50.0);

void fragment() {
    vec2 pixel = floor(UV * chunk_size);
    vec2 uv = (pixel + 0.5) / chunk_size;

    vec4 data = texture(block_data, uv);
    float light = data.r;

    float darkness = (light < 0.01) ? 1.0 : 1.0 - light;

    COLOR = vec4(0.0, 0.0, 0.0, darkness);
}
