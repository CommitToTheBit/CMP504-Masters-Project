shader_type canvas_item;

uniform float textWidth = 128.0;
uniform float hover = 0.0;
uniform float scale = 1.0;

void fragment() {
	
	// STEP 1: Calculate each pixel's 'radius'...
	float nearestX = max(32.0, min(textWidth * UV.x, textWidth - 32.0));
	float radius = length(vec2(textWidth, 64.0) * UV - vec2(nearestX, 32.0));
	
	// STEP 2: Colour outline based on radius...
	float blur = 1.25 * scale;
	float minRadius = 26.5 * scale;
	float maxRadius = (32.0 - blur) * scale;
	
	float t = 1.0;
	if (radius < minRadius)
		t = max((radius-(minRadius-blur))/blur, hover);
	else if (radius > maxRadius)
		t = max(((maxRadius+blur)-radius)/blur, 0.0);
	
	COLOR = vec4(0.0f, 214.0f / 255.0f, 1.0f, t);
}
