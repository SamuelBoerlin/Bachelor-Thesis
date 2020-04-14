#version 120

varying vec3 v_vertexPosition;
varying vec3 v_vertexWorldPosition;
varying vec3 v_vertexNormal;
varying vec3 v_vertexWorldNormal;
varying vec4 v_vertexColor;
varying vec3 v_lightPos;

uniform mat4 u_viewMatrix;
uniform mat4 u_inverseViewMatrix;
uniform sampler2D u_texture;
uniform float u_saliencyOverlay;

void main()
{
	vec3 N = normalize(v_vertexNormal);
	
	vec3 E = normalize(-v_vertexPosition);
	vec3 L = normalize(v_lightPos - v_vertexPosition);
	vec3 H = normalize(-reflect(L, N));
	
	vec3 lightAmbient = vec3(0.5, 0.5, 0.5);
	vec3 lightDiffuse = vec3(0.85, 0.85, 0.85);
	vec3 lightSpecular = vec3(0.35, 0.35, 0.35);
	
	float specularShininess = 2;
	
	vec3 ambient = lightAmbient;
	vec3 diffuse = lightDiffuse * max(dot(L, N), 0);
	vec3 specular = lightSpecular * pow(max(dot(H, E), 0), specularShininess);

	vec4 shadedVertexColor = vec4(v_vertexColor.rgb * (ambient + diffuse + specular), v_vertexColor.a);
	vec4 shadedTextureColor = vec4(1, 1, 1, 1);
	if(u_saliencyOverlay < 0.99) {
		vec4 textureDiffuse = texture2D(u_texture, gl_TexCoord[0].st);
		shadedTextureColor = vec4(textureDiffuse.rgb * (ambient + diffuse + specular), textureDiffuse.a);
	}

	gl_FragColor = mix(shadedTextureColor, shadedVertexColor, u_saliencyOverlay);
}