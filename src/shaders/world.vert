#version 120

uniform vec4 u_eyePos;
uniform vec4 u_lightPos;
uniform mat4 u_viewMatrix;
uniform mat4 u_inverseViewMatrix;

varying vec3 v_vertexPosition;
varying vec3 v_vertexWorldPosition;
varying vec3 v_vertexNormal;
varying vec3 v_vertexWorldNormal;
varying vec4 v_vertexColor;
varying vec3 v_lightPos;

void main()
{
	v_vertexColor = gl_Color;
	v_vertexNormal = vec3(gl_NormalMatrix * gl_Normal);
	v_vertexWorldNormal = vec3(u_inverseViewMatrix * vec4(gl_NormalMatrix * gl_Normal, 0));
	v_vertexPosition = vec3(gl_ModelViewMatrix * gl_Vertex);
	v_vertexWorldPosition = vec3(u_inverseViewMatrix * gl_ModelViewMatrix * gl_Vertex);
	v_lightPos = vec3(u_viewMatrix * u_lightPos);
	
	gl_TexCoord[0] = gl_MultiTexCoord0;
	gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
}
