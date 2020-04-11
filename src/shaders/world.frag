#version 120

varying vec3 v_vertexPosition;
varying vec3 v_vertexWorldPosition;
varying vec3 v_vertexNormal;
varying vec3 v_vertexWorldNormal;
varying vec4 v_vertexColor;
varying vec3 v_lightPos;

uniform mat4 u_viewMatrix;
uniform mat4 u_inverseViewMatrix;

void main()
{
	vec3 N = normalize(v_vertexNormal);
	
	vec3 E = normalize(-v_vertexPosition);
	vec3 L = normalize(v_lightPos - v_vertexPosition);
	vec3 H = normalize(-reflect(L, N));
	
	vec3 lightAmbient = vec3(0.4, 0.4, 0.4);
	vec3 lightDiffuse = vec3(0.85, 0.85, 0.85);
	vec3 lightSpecular = vec3(0.5, 0.5, 0.5);
	
	float specularShininess = 1;
	
	vec3 ambient = lightAmbient;
	vec3 diffuse = lightDiffuse * max(dot(L, N), 0);
	vec3 specular = lightSpecular * pow(max(dot(H, E), 0), specularShininess);

	gl_FragColor = vec4(v_vertexColor.rgb * (ambient + diffuse + specular), v_vertexColor.a);
}