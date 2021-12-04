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
    class Cuboid : Volume
    {
        public Cuboid()
        {
            VertCount = 8;
            IndiceCount = 36;
            ColorDataCount = 8;
        }

        public override Vector3[] GetVerts()
        {
            return new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f)
            };
        }

        public override int[] GetIndices(int offset = 0)
        {
            int[] inds = new int[]
            {
                0, 2, 1,
                0, 3, 2,
                1, 2, 6,
                6, 5, 1,
                4, 5, 6,
                6, 7, 4,
                2, 3, 6,
                6, 3, 7,
                0, 7, 3,
                0, 4, 7,
                0, 1, 5,
                0, 5, 4
            };

            if (offset != 0)
            {
                for (int i = 0; i < inds.Length; i++)
                {
                    inds[i] += offset;
                }
            }

            return inds;
        }

        public override Vector3[] GetColorData()
        {
            return new Vector3[]
            {
                new Vector3(1.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 1.0f),
                new Vector3(0.0f, 1.0f, 0.0f),
                new Vector3(1.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 1.0f),
                new Vector3(0.0f, 1.0f, 0.0f),
                new Vector3(1.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 1.0f)
            };
        }

        public override void CalculateModelMatrix()
        {
            ModelMatrix = Matrix4.CreateScale(Scale) * Matrix4.CreateRotationX(Rotation.X) * Matrix4.CreateRotationY(Rotation.Y) * Matrix4.CreateRotationZ(Rotation.Z) * Matrix4.CreateTranslation(Position);
        }

        public override Vector2[] GetTextureCoords()
        {
            return new Vector2[] { };
        }
    }
    
    public class Plane : Volume
    {
        public Vector3[] vertices = new Vector3[4];
        
        public Plane(Vector3[] Vertices)
        {
            vertices = Vertices;
            VertCount = 4;
            IndiceCount = 6;
            ColorDataCount = 4;
        }

        public override Vector3[] GetVerts()
        {
            return vertices;
        }

        public override int[] GetIndices(int offset = 0)
        {
            int[] inds = new int[]
            {
                0, 1, 2,
                2, 3, 0
            };

            if (offset != 0)
            {
                for (int i = 0; i < inds.Length; i++)
                {
                    inds[i] += offset;
                }
            }

            return inds;
        }

        public override Vector3[] GetColorData()
        {
            return new Vector3[]
            {
                new Vector3(1.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 1.0f),
                new Vector3(0.0f, 1.0f, 0.0f),
                new Vector3(1.0f, 0.0f, 0.0f)
            };
        }
        
        public override void CalculateModelMatrix()
        {
            ModelMatrix = Matrix4.CreateScale(Scale) * Matrix4.CreateRotationX(Rotation.X) *
                          Matrix4.CreateRotationY(Rotation.Y) * Matrix4.CreateRotationZ(Rotation.Z) *
                          Matrix4.CreateTranslation(Position);
        }

        public override Vector2[] GetTextureCoords()
        {
            return new Vector2[] { };
        }
    }
}
