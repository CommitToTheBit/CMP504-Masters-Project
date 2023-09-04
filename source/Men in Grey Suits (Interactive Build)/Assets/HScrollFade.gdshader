shader_type canvas_item;

void fragment() 
{
	vec4 t = texture(TEXTURE,UV);
	
	float lowerBound = 0.05;
	float upperBound = 0.95;
	
	if (UV.x < lowerBound)
	{
		t.a = pow((lowerBound - UV.x) / lowerBound, 2.0);
	}
	else if (UV.x <= upperBound) 
	{
		t.a = 0.0;
	} 
	else 
	{
		t.a = pow((UV.x - upperBound) / (1.0 - upperBound), 2.0);
	}
	COLOR = t;
}