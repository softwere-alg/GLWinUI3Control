#version 330 core

// vertex position
attribute vec3 position;

// model matrix
uniform mat4 modelMatrix;

void main()
{
    gl_Position = modelMatrix * vec4(position, 1.0);
}
