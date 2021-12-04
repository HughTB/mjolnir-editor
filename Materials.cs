using System;
using System.Linq;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenTK.Input;

namespace mjolnir_editor
{
    public class Material
    {
        public Vector3 AmbientColor = new Vector3();
        public Vector3 DiffuseColor = new Vector3();
        public Vector3 SpecularColor = new Vector3();
        public float SpecularExponent = 1;
        public float Opacity = 1.0f;

        public string AmbientMap = "";
        public string DiffuseMap = "";
        public string SpecularMap = "";
        public string OpacityMap = "";
        public string NormalMap = "";

        public Material() { }

        public Material(Vector3 ambient, Vector3 diffuse, Vector3 specular, float specexponent = 1.0f, float opacity = 1.0f)
        {
            AmbientColor = ambient;
            DiffuseColor = diffuse;
            SpecularColor = specular;
            SpecularExponent = specexponent;
            Opacity = opacity;
        }

        public static Dictionary<string, Material> LoadFromFile(string filename)
        {
            Dictionary<string, Material> mats = new Dictionary<string, Material>();

            try
            {
                string currentmat = "";

                using (StreamReader sr = new StreamReader(new FileStream(filename, FileMode.Open, FileAccess.Read)))
                {
                    string currentline;

                    while (!sr.EndOfStream)
                    {
                        currentline = sr.ReadLine();

                        if (!currentline.StartsWith("newmtl")) { if (currentmat.StartsWith("newmtl")) { currentmat += currentline + "\n"; } }
                        else
                        {
                            if (currentmat.Length > 0)
                            {
                                Material newMat = new Material();
                                string newMatName = "";

                                newMat = LoadFromString(currentmat, out newMatName);
                                mats.Add(newMatName, newMat);
                            }

                            currentmat = currentline + "\n";
                        }
                    }
                }

                if (currentmat.Count((char c) => c == '\n') > 0)
                {
                    Material newMat = new Material();
                    string newMatName = "";

                    newMat = LoadFromString(currentmat, out newMatName);
                    mats.Add(newMatName, newMat);
                }
            } catch (FileNotFoundException e) { Console.WriteLine($"File not found: {filename}"); }
            catch (Exception e) { Console.WriteLine($"Error loading file: {e}"); }

            return mats;
        }

        public static Material LoadFromString(string mat, out string name)
        {
            Material output = new Material();
            name = "";

            List<string> lines = mat.Split('\n').ToList();
            lines = lines.SkipWhile(s => !s.StartsWith("newmtl ")).ToList();

            if (lines.Count != 0)
            {
                name = lines[0].Substring("newmtl ".Length);
            }

            lines = lines.Select((string s) => s.Trim()).ToList();

            foreach (string line in lines)
            {
                if (line.Length < 3 || line.StartsWith("//") || line.StartsWith("#")) { continue; }

                if (line.StartsWith("Ka"))
                {
                    string[] colorparts = line.Substring(3).Split(' ');

                    if (colorparts.Length < 3) { throw new ArgumentException("Invalid colour data"); }

                    Vector3 vec = new Vector3();

                    bool success = float.TryParse(colorparts[0], out vec.X);
                    success &= float.TryParse(colorparts[1], out vec.Y);
                    success &= float.TryParse(colorparts[2], out vec.Z);

                    output.AmbientColor = vec;

                    if (!success) { Console.WriteLine($"Error parsing ambient colour: {line}"); }
                } else if (line.StartsWith("Kd"))
                {
                    string[] colorparts = line.Substring(3).Split(' ');

                    if (colorparts.Length < 3) { throw new ArgumentException("Invalid colour data"); }

                    Vector3 vec = new Vector3();

                    bool success = float.TryParse(colorparts[0], out vec.X);
                    success &= float.TryParse(colorparts[1], out vec.Y);
                    success &= float.TryParse(colorparts[2], out vec.Z);

                    output.DiffuseColor = vec;

                    if (!success) { Console.WriteLine($"Error parsing diffuse colour: {line}"); }
                } else if (line.StartsWith("Ks"))
                {
                    string[] colorparts = line.Substring(3).Split(' ');

                    if (colorparts.Length < 3) { throw new ArgumentException("Invalid colour data"); }

                    Vector3 vec = new Vector3();

                    bool success = float.TryParse(colorparts[0], out vec.X);
                    success &= float.TryParse(colorparts[1], out vec.Y);
                    success &= float.TryParse(colorparts[2], out vec.Z);

                    output.SpecularColor = vec;

                    if (!success) { Console.WriteLine($"Error parsing specular colour: {line}"); }
                } else if (line.StartsWith("Ns"))
                {
                    float exponent = 0.0f;
                    bool success = float.TryParse(line.Substring(3), out exponent);

                    output.SpecularExponent = exponent;

                    if (!success) { Console.WriteLine($"Error parsing specular exponent: {line}"); }
                } else if (line.StartsWith("map_Ka"))
                {
                    if (line.Length > "map_Ka".Length + 6)
                    {
                        output.AmbientMap = line.Substring("map_Ka".Length + 1);
                    }
                } else if (line.StartsWith("map_Kd"))
                {
                    if (line.Length > "map_Kd".Length + 6)
                    {
                        output.DiffuseMap = line.Substring("map_Kd".Length + 1);
                    }
                } else if (line.StartsWith("map_Ks"))
                {
                    if (line.Length > "map_Ks".Length + 6)
                    {
                        output.SpecularMap = line.Substring("map_Ks".Length + 1);
                    }
                } else if (line.StartsWith("map_normal"))
                {
                    if (line.Length > "map_normal".Length + 6)
                    {
                        output.NormalMap = line.Substring("map_normal".Length + 1);
                    }
                } else if (line.StartsWith("map_opacity"))
                {
                    if (line.Length > "map_opacity".Length + 6)
                    {
                        output.OpacityMap = line.Substring("map_opacity".Length + 1);
                    }
                }
            }

            return output;
        }
    }
}
