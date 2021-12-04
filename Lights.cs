using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.IO;
using OpenTK.Input;
namespace mjolnir_editor
{
    public class Light
    {
        public Vector3 Position;
        public Vector3 Color = new Vector3();
        public int id;

        public float DiffuseIntensity = 1.0f;
        public float AmbientIntensity = 0.1f;

        public float LinearAttenuation;
        public float QuadraticAttenuation;

        public LightType Type;
        public Vector3 Direction;
        public float ConeAngle;

        public Light(int id, Vector3 position, Vector3 color, float diffuseIntensity = 1.0f, float ambientIntensity = 0.1f)
        {
            this.id = id;
            
            Position = position;
            Color = color;

            DiffuseIntensity = diffuseIntensity;
            AmbientIntensity = ambientIntensity;

            Type = LightType.Point;
            Direction = new Vector3(0.0f, 0.0f, 1.0f);
            ConeAngle = 15.0f;
        }
    }

    public enum LightType { Point, Spot, Directional }
}
