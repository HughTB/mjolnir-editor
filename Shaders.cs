using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.IO;
using OpenTK.Input;
using System.Text;

namespace mjolnir_editor
{
    class ShaderProgram
    {
        public int ProgramID = -1;
        public int VShaderID = -1;
        public int FShaderID = -1;
        public int AttributeCount = 0;
        public int UniformCount = 0;

        public Dictionary<string, AttributeInfo> Attributes = new Dictionary<string, AttributeInfo>();

        public Dictionary<string, UniformInfo> Uniforms = new Dictionary<string, UniformInfo>();

        public Dictionary<string, uint> Buffers = new Dictionary<string, uint>();

        public ShaderProgram()
        {
            ProgramID = GL.CreateProgram();
        }

        public ShaderProgram(string vshader, string fshader, bool fromFile = false)
        {
            ProgramID = GL.CreateProgram();

            if (fromFile)
            {
                loadShaderFromFile(vshader, ShaderType.VertexShader);
                loadShaderFromFile(fshader, ShaderType.FragmentShader);
            } else
            {
                loadShaderFromString(vshader, ShaderType.VertexShader);
                loadShaderFromString(fshader, ShaderType.FragmentShader);
            }

            link();
            genBuffers();
        }

        private void loadShader(string code, ShaderType type, out int address)
        {
            address = GL.CreateShader(type);
            GL.ShaderSource(address, code);
            GL.CompileShader(address);
            GL.AttachShader(ProgramID, address);
            Console.WriteLine(GL.GetShaderInfoLog(address));
        }

        public void loadShaderFromString(string code, ShaderType type)
        {
            if (type == ShaderType.VertexShader)
            {
                loadShader(code, type, out VShaderID);
            } else if (type == ShaderType.FragmentShader)
            {
                loadShader(code, type, out FShaderID);
            }
        }

        public void loadShaderFromFile(string filename, ShaderType type)
        {
            using (StreamReader sr = new StreamReader(filename))
            {
                if (type == ShaderType.VertexShader)
                {
                    loadShader(sr.ReadToEnd(), type, out VShaderID);
                } else if (type == ShaderType.FragmentShader)
                {
                    loadShader(sr.ReadToEnd(), type, out FShaderID);
                }
            }
        }

        public void link()
        {
            GL.LinkProgram(ProgramID);

            Console.WriteLine(GL.GetProgramInfoLog(ProgramID));

            GL.GetProgram(ProgramID, ProgramParameter.ActiveAttributes, out AttributeCount);
            GL.GetProgram(ProgramID, ProgramParameter.ActiveUniforms, out UniformCount);

            for (int i = 0; i < AttributeCount; i++)
            {
                AttributeInfo info = new AttributeInfo();
                int length = 0;

                string name;

                GL.GetActiveAttrib(ProgramID, i, 256, out length, out info.size, out info.type, out name);

                info.name = name;
                info.address = GL.GetAttribLocation(ProgramID, info.name);
                Attributes.Add(name, info);
            }

            for (int i = 0; i < UniformCount; i++)
            {
                UniformInfo info = new UniformInfo();
                int length = 0;

                string name;

                GL.GetActiveUniform(ProgramID, i, 256, out length, out info.size, out info.type, out name);

                info.name = name;
                Uniforms.Add(name, info);
                info.address = GL.GetUniformLocation(ProgramID, info.name);
            }
        }

        public void genBuffers()
        {
            AttributeInfo[] attributeInfos = new AttributeInfo[Attributes.Count];
            Attributes.Values.CopyTo(attributeInfos, 0);

            for (int i = 0; i < Attributes.Count; i++)
            {
                uint buffer = 0;
                GL.GenBuffers(1, out buffer);

                Buffers.Add(attributeInfos[i].name, buffer);
            }

            UniformInfo[] uniformInfos = new UniformInfo[Uniforms.Count];
            Uniforms.Values.CopyTo(uniformInfos, 0);

            for (int i = 0; i < Uniforms.Count; i++)
            {
                uint buffer = 0;
                GL.GenBuffers(1, out buffer);

                Buffers.Add(uniformInfos[i].name, buffer);
            }
        }

        public void enableVertexAttribArrays()
        {
            AttributeInfo[] attributeInfos = new AttributeInfo[Attributes.Count];
            Attributes.Values.CopyTo(attributeInfos, 0);

            for (int i = 0; i < Attributes.Count; i++)
            {
                GL.EnableVertexAttribArray(attributeInfos[i].address);
            }
        }

        public void disableVertexAttribArrays()
        {
            AttributeInfo[] attributeInfos = new AttributeInfo[Attributes.Count];
            Attributes.Values.CopyTo(attributeInfos, 0);

            for (int i = 0; i < Attributes.Count; i++)
            {
                GL.DisableVertexAttribArray(attributeInfos[i].address);
            }
        }

        public int getAttribute(string name)
        {
            if (Attributes.ContainsKey(name))
            {
                return Attributes[name].address;
            } else { return -1; }
        }

        public int getUniform(string name)
        {
            if (Uniforms.ContainsKey(name))
            {
                return Uniforms[name].address;
            } else { return -1; }
        }

        public uint getBuffer(string name)
        {
            if (Buffers.ContainsKey(name))
            {
                return Buffers[name];
            } else { return 0; }
        }
    }

    class AttributeInfo
    {
        public string name = "";
        public int address = -1;
        public int size = 0;
        public ActiveAttribType type;
    }

    class UniformInfo
    {
        public string name = "";
        public int address = -1;
        public int size = 0;
        public ActiveUniformType type;
    }
}
