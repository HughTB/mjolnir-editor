using System;
using System.Linq;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.IO;
using OpenTK.Input;

namespace mjolnir_editor
{
    class ObjVolume : Volume
    {
        Vector3[] verticies;
        Vector3[] colors;
        Vector2[] texturecoords;

        List<Tuple<FaceVertex, FaceVertex, FaceVertex>> faces = new List<Tuple<FaceVertex, FaceVertex, FaceVertex>>();

        public override int VertCount { get { return verticies.Length; } }
        public override int IndiceCount { get { return faces.Count * 3; } }
        public override int ColorDataCount { get { return colors.Length; } }

        public override Vector3[] GetVerts()
        {
            List<Vector3> verts = new List<Vector3>();

            foreach (var face in faces)
            {
                verts.Add(face.Item1.Position);
                verts.Add(face.Item2.Position);
                verts.Add(face.Item3.Position);
            }

            return verts.ToArray();
        }

        public override int[] GetIndices(int offset = 0)
        {
            return Enumerable.Range(offset, IndiceCount).ToArray();
        }

        public override Vector3[] GetColorData()
        {
            return new Vector3[ColorDataCount];
        }

        public override Vector2[] GetTextureCoords()
        {
            List<Vector2> coords = new List<Vector2>();

            foreach (var face in faces)
            {
                coords.Add(face.Item1.TextureCoord);
                coords.Add(face.Item2.TextureCoord);
                coords.Add(face.Item3.TextureCoord);
            }

            return coords.ToArray();
        }

        public override void CalculateModelMatrix()
        {
            ModelMatrix = Matrix4.CreateScale(Scale) * Matrix4.CreateRotationX(Rotation.X) * Matrix4.CreateRotationY(Rotation.Y) * Matrix4.CreateRotationZ(Rotation.Z) * Matrix4.CreateTranslation(Position);
        }

        public static ObjVolume LoadFromFile(string filename)
        {
            ObjVolume obj = new ObjVolume();

            try
            {
                using (StreamReader sr = new StreamReader(filename))
                {
                    obj = LoadFromString(sr.ReadToEnd());
                }
            }
            catch (FileNotFoundException e) { Console.WriteLine($"File not found: {filename}"); }
            catch (Exception e) { Console.WriteLine($"Error occured while loading model: {e}"); }

            return obj;
        }

        public static ObjVolume LoadFromString(string obj)
        {
            List<string> lines = new List<string>(obj.Split('\n'));

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> texts = new List<Vector2>();
            List<Tuple<TempVertex, TempVertex, TempVertex>> faces = new List<Tuple<TempVertex, TempVertex, TempVertex>>();

            verts.Add(new Vector3());
            texts.Add(new Vector2());
            normals.Add(new Vector3());

            foreach (string line in lines)
            {
                if (line.StartsWith("v "))
                {
                    string temp = line.Substring(2);

                    Vector3 vec = new Vector3();

                    if (temp.Count((char c) => c == ' ') == 2)
                    {
                        string[] vertparts = temp.Split(' ');

                        bool success = float.TryParse(vertparts[0], out vec.X);
                        success &= float.TryParse(vertparts[1], out vec.Y);
                        success &= float.TryParse(vertparts[2], out vec.Z);

                        if (!success) { Console.WriteLine($"Error parsing vertex: {line}"); }
                    }
                    else { Console.WriteLine($"Error parsing vertex: {line}"); }

                    verts.Add(vec);
                }
                else if (line.StartsWith("vt "))
                {
                    string temp = line.Substring(2);

                    Vector2 vec = new Vector2();

                    if (temp.Trim().Count((char c) => c == ' ') > 0)
                    {
                        string[] texcoordparts = temp.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        bool success = float.TryParse(texcoordparts[0], out vec.X);
                        success &= float.TryParse(texcoordparts[1], out vec.Y);

                        if (!success) { Console.WriteLine($"Error parsing texture coordinate: {line}"); }
                    }
                    else { Console.WriteLine($"Error parsing texture coordinate: {line}"); }

                    texts.Add(vec);
                }
                else if (line.StartsWith("vn "))
                {
                    string temp = line.Substring(2);

                    Vector3 vec = new Vector3();

                    if (temp.Trim().Count((char c) => c == ' ') == 2)
                    {
                        string[] vertparts = temp.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        bool success = float.TryParse(vertparts[0], out vec.X);
                        success &= float.TryParse(vertparts[1], out vec.Y);
                        success &= float.TryParse(vertparts[2], out vec.Z);

                        if (!success) { Console.WriteLine($"Error parsing normal: {line}"); }
                    }
                    else { Console.WriteLine($"Error parsing normal: {line}"); }

                    normals.Add(vec);
                }
                else if (line.StartsWith("f "))
                {
                    string temp = line.Substring(2);

                    Tuple<TempVertex, TempVertex, TempVertex> face = new Tuple<TempVertex, TempVertex, TempVertex>(new TempVertex(), new TempVertex(), new TempVertex());

                    if (temp.Count((char c) => c == ' ') == 2)
                    {
                        string[] faceparts = temp.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        int v1, v2, v3;
                        int t1, t2, t3;
                        int n1, n2, n3;

                        bool success = int.TryParse(faceparts[0].Split('/')[0], out v1);
                        success &= int.TryParse(faceparts[1].Split('/')[0], out v2);
                        success &= int.TryParse(faceparts[2].Split('/')[0], out v3);

                        if (faceparts[0].Count((char c) => c == '/') >= 2)
                        {
                            success &= int.TryParse(faceparts[0].Split('/')[1], out t1);
                            success &= int.TryParse(faceparts[1].Split('/')[1], out t2);
                            success &= int.TryParse(faceparts[2].Split('/')[1], out t3);
                            success &= int.TryParse(faceparts[0].Split('/')[2], out n1);
                            success &= int.TryParse(faceparts[1].Split('/')[2], out n2);
                            success &= int.TryParse(faceparts[2].Split('/')[2], out n3);
                        }
                        else
                        {
                            if (texts.Count > v1 && texts.Count > v2 && texts.Count > v3) { t1 = v1; t2 = v2; t3 = v3; }
                            else { t1 = 0; t2 = 0; t3 = 0; }

                            if (normals.Count > v1 && normals.Count > v2 && normals.Count > v3) { n1 = v1; n2 = v2; n3 = v3; }
                            else { n1 = 0; n2 = 0; n3 = 0; }
                        }

                        if (!success) { Console.WriteLine($"Error parsing face: {line}"); }
                        else
                        {
                            TempVertex tv1 = new TempVertex(v1, n2, t3);
                            TempVertex tv2 = new TempVertex(v2, n2, t2);
                            TempVertex tv3 = new TempVertex(v3, n3, t3);

                            face = new Tuple<TempVertex, TempVertex, TempVertex>(tv1, tv2, tv3);
                            faces.Add(face);
                        }
                    }
                    else { Console.WriteLine($"Error parsing face: {line}"); }
                }
            }

            ObjVolume vol = new ObjVolume();

            foreach (var face in faces)
            {
                FaceVertex v1 = new FaceVertex(verts[face.Item1.Vertex], normals[face.Item1.Vertex], texts[face.Item1.Vertex]);
                FaceVertex v2 = new FaceVertex(verts[face.Item2.Vertex], normals[face.Item2.Vertex], texts[face.Item2.Vertex]);
                FaceVertex v3 = new FaceVertex(verts[face.Item3.Vertex], normals[face.Item3.Vertex], texts[face.Item3.Vertex]);

                vol.faces.Add(new Tuple<FaceVertex, FaceVertex, FaceVertex>(v1, v2, v3));
            }

            return vol;
        }

        private class TempVertex
        {
            public int Vertex;
            public int Normal;
            public int Texcoord;

            public TempVertex(int vert = 0, int norm = 0, int tex = 0)
            {
                Vertex = vert;
                Normal = norm;
                Texcoord = tex;
            }
        }

        public override Vector3[] GetNormals()
        {
            if (base.GetNormals().Length > 0) { return base.GetNormals(); }

            List<Vector3> normals = new List<Vector3>();

            foreach (var face in faces)
            {
                normals.Add(face.Item1.Normal);
                normals.Add(face.Item2.Normal);
                normals.Add(face.Item3.Normal);
            }

            return normals.ToArray();
        }

        public override int NormalCount { get { return faces.Count * 3; } }
    }

    class FaceVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TextureCoord;

        public FaceVertex(Vector3 pos, Vector3 norm, Vector2 texcoord)
        {
            Position = pos; Normal = norm; TextureCoord = texcoord;
        }
    }
}
