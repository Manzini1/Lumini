shader_type canvas_item;

uniform vec4 top_color    : source_color = vec4(0.5725, 0.1843, 0.5216, 1.0); // #922f85
uniform vec4 bottom_color : source_color = vec4(0.1608, 0.5020, 0.6706, 1.0); // #2980ab

uniform float alpha_top    : hint_range(0.0, 1.0) = 1.00; // em cima
uniform float alpha_bottom : hint_range(0.0, 1.0) = 0.1; // em baixo
uniform float power : hint_range(0.1, 8.0) = 1.5;         // curva do gradiente

void fragment() {
    // UV.x vai de 0 (início da linha) até 1 (fim da linha)
    float t = pow(UV.x, power);

    vec4 col = mix(top_color, bottom_color, t);

    float a = mix(alpha_top, alpha_bottom, t);

    // Mantém multiplicação pelo alpha original (se você usar modulate/default_color)
    COLOR = vec4(col.rgb, col.a * a) * COLOR;
}
