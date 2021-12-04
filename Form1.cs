using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace mjolnir_editor
{
    public partial class Form1 : Form
    {
        GLControl glCanvas;

        //Other definitions
        Dictionary<string, ShaderProgram> shaders = new Dictionary<string, ShaderProgram>();
        Dictionary<string, int> textures = new Dictionary<string, int>();
        Dictionary<string, Material> materials = new Dictionary<string, Material>();

        private World world = new World();
        private string currentFile = null;
        private bool controlCamera = false;

        string activeShader = "default";
        private Vector2 lastMousePos = new Vector2();

        int ibo_elements;

        Vector3[] vertdata;
        Vector3[] coldata;
        Vector2[] texcoorddata;
        Vector3[] normdata;
        List<Volume> objects = new List<Volume>();
        int[] indicedata;
        Matrix4 view = Matrix4.Identity;

        private Timer _timer;
        Stopwatch stopwatch = new Stopwatch();
        float time = 0.0f;

        Camera cam = new Camera();

        List<Light> lights = new List<Light>();
        const int MAX_LIGHTS = 5;

        public Form1()
        {
            InitializeComponent();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(this.Form1_KeyDown);

            //Graphics Settings
            int aaSamples = 4;
            int ups = 240;
            int colourDepth = Screen.PrimaryScreen.BitsPerPixel;

            //Load application settings
            //showConsoleToolStripMenuItem.Checked = Properties.Settings.Default.ShowConsole;

            glCanvas = new GLControl(new GraphicsMode(colourDepth, 24, 0, aaSamples));

            glCanvas.Dock = DockStyle.Fill;

            Controls.Add(glCanvas);

            glCanvas.Resize += GlCanvas_Resize;
            glCanvas.Paint += GlCanvas_Paint;

            InitProgram();

            GL.ClearColor(Color.CornflowerBlue);
            GL.PointSize(5.0f);

            _timer = new Timer();
            _timer.Tick += (_sender, _e) =>
            {
                stopwatch.Stop();
                float deltaTime = (float)stopwatch.ElapsedMilliseconds / 1000;
                time += deltaTime;
                stopwatch.Restart();

                GlCanvas_Update(deltaTime);
                GlCanvas_Render();
            };

            _timer.Interval = 1000 / ups;
            _timer.Start();

            stopwatch.Start();
        }

        //First opengl code called, all initialisations should be called here
        void InitProgram()
        {
            glCanvas.VSync = true;
            vSyncToolStripMenuItem.Checked = true;

            lastMousePos = new Vector2(Cursor.Position.X, Cursor.Position.Y);
            
            GL.GenBuffers(1, out ibo_elements);

            LoadResources();

            activeShader = "lit";

            SetupScene();
        }

        private void ProcessInput(float deltaTime)
        {
            //Process mouse input
            Vector2 delta = lastMousePos - new Vector2(Cursor.Position.X, Cursor.Position.Y);

            cam.AddRotation(delta.X * deltaTime, delta.Y * deltaTime);

            Cursor.Position = new Point(this.Location.X + this.Size.Width / 2, this.Location.Y + this.Size.Height / 2);
            lastMousePos = new Vector2(Cursor.Position.X, Cursor.Position.Y);

            //Process keyboard input
            if (Keyboard.GetState().IsKeyDown(Key.W))
            {
                cam.Move(0.0f, cam.moveSpeed, 0.0f);
            }
            if (Keyboard.GetState().IsKeyDown(Key.S))
            {
                cam.Move(0.0f, -cam.moveSpeed, 0.0f);
            }
            if (Keyboard.GetState().IsKeyDown(Key.A))
            {
                cam.Move(-cam.moveSpeed, 0.0f, 0.0f);
            }
            if (Keyboard.GetState().IsKeyDown(Key.D))
            {
                cam.Move(cam.moveSpeed, 0.0f, 0.0f);
            }
            if (Keyboard.GetState().IsKeyDown(Key.E))
            {
                cam.Move(0.0f, 0.0f, cam.moveSpeed);
            }
            if (Keyboard.GetState().IsKeyDown(Key.Q))
            {
                cam.Move(0.0f, 0.0f, -cam.moveSpeed);
            }
        }
        
        private void GlCanvas_Update(float deltaTime)
        {
            List<Vector3> verts = new List<Vector3>();
            List<int> inds = new List<int>();
            List<Vector3> colors = new List<Vector3>();
            List<Vector2> texcoords = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();

            if (controlCamera)
            {
                ProcessInput(deltaTime);   
            }

            int vertcount = 0;

            foreach (Volume v in objects)
            {
                verts.AddRange(v.GetVerts());
                inds.AddRange(v.GetIndices(vertcount));
                colors.AddRange(v.GetColorData());
                texcoords.AddRange(v.GetTextureCoords());
                normals.AddRange(v.GetNormals());
                vertcount += v.VertCount;
            }

            vertdata = verts.ToArray();
            indicedata = inds.ToArray();
            coldata = colors.ToArray();
            texcoorddata = texcoords.ToArray();
            normdata = normals.ToArray();

            GL.BindBuffer(BufferTarget.ArrayBuffer, shaders[activeShader].getBuffer("vPosition"));
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(vertdata.Length * Vector3.SizeInBytes), vertdata, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(shaders[activeShader].getAttribute("vPosition"), 3, VertexAttribPointerType.Float, false, 0, 0);

            if (shaders[activeShader].getAttribute("vColor") != -1)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, shaders[activeShader].getBuffer("vColor"));
                GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(coldata.Length * Vector3.SizeInBytes), coldata, BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(shaders[activeShader].getAttribute("vColor"), 3, VertexAttribPointerType.Float, true, 0, 0);
            }

            if (shaders[activeShader].getAttribute("texcoord") != -1)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, shaders[activeShader].getBuffer("texcoord"));
                GL.BufferData<Vector2>(BufferTarget.ArrayBuffer, (IntPtr)(texcoorddata.Length * Vector2.SizeInBytes), texcoorddata, BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(shaders[activeShader].getAttribute("texcoord"), 2, VertexAttribPointerType.Float, true, 0, 0);
            }

            if (shaders[activeShader].getAttribute("vNormal") != -1)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, shaders[activeShader].getBuffer("vNormal"));
                GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(normdata.Length * Vector3.SizeInBytes), normdata, BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(shaders[activeShader].getAttribute("vNormal"), 3, VertexAttribPointerType.Float, true, 0, 0);
            }

            /*objects[0].Position = new Vector3(0.3f, -0.5f + (float)Math.Sin(time), -3.0f);
            objects[0].Rotation = new Vector3(0.5f * time, 0.25f * time, 0);
            objects[0].Scale = new Vector3(1.0f, 1.0f, 1.0f);

            objects[1].Position = new Vector3(-1.0f, 0.5f + (float)Math.Cos(time), -2.0f);
            objects[1].Rotation = new Vector3(-0.25f * time, -0.35f * time, 0);
            objects[1].Scale = new Vector3(1.0f, 1.0f, 1.0f);*/

            foreach (Volume v in objects)
            {
                v.CalculateModelMatrix();
                v.ViewProjectionMatrix = cam.GetViewMatrix() * Matrix4.CreatePerspectiveFieldOfView(1.3f, ClientSize.Width / (float)ClientSize.Height, 1.0f, 40.0f);
                v.ModelViewProjectionMatrix = v.ModelMatrix * v.ViewProjectionMatrix;
            }

            GL.UseProgram(shaders[activeShader].ProgramID);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo_elements);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(indicedata.Length * sizeof(int)), indicedata, BufferUsageHint.StaticDraw);

            view = cam.GetViewMatrix();
        }

        private void GlCanvas_Render()
        {
            GL.Viewport(0, 0, Width, Height);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.DepthTest);
            //Actual drawing code

            GL.UseProgram(shaders[activeShader].ProgramID);
            shaders[activeShader].enableVertexAttribArrays();

            int indiceat = 0;

            foreach (Volume v in objects)
            {
                GL.BindTexture(TextureTarget.Texture2D, v.TextureID);

                GL.UniformMatrix4(shaders[activeShader].getUniform("modelview"), false, ref v.ModelViewProjectionMatrix);

                if (shaders[activeShader].getAttribute("maintexture") != -1) { GL.Uniform1(shaders[activeShader].getAttribute("maintexture"), v.TextureID); }

                if (shaders[activeShader].getUniform("view") != -1) { GL.UniformMatrix4(shaders[activeShader].getUniform("view"), false, ref view); }

                if (shaders[activeShader].getUniform("model") != -1) { GL.UniformMatrix4(shaders[activeShader].getUniform("model"), false, ref v.ModelMatrix); }

                if (shaders[activeShader].getUniform("material_ambient") != -1) { GL.Uniform3(shaders[activeShader].getUniform("material_ambient"), ref v.Material.AmbientColor); }

                if (shaders[activeShader].getUniform("material_diffuse") != -1) { GL.Uniform3(shaders[activeShader].getUniform("material_diffuse"), ref v.Material.DiffuseColor); }

                if (shaders[activeShader].getUniform("material_specular") != -1) { GL.Uniform3(shaders[activeShader].getUniform("material_specular"), ref v.Material.SpecularColor); }

                if (shaders[activeShader].getUniform("material_specExponent") != -1) { GL.Uniform1(shaders[activeShader].getUniform("material_specExponent"), v.Material.SpecularExponent); }

                if (shaders[activeShader].getUniform("light_position") != -1) { GL.Uniform3(shaders[activeShader].getUniform("light_position"), ref lights[0].Position); }

                if (shaders[activeShader].getUniform("light_color") != -1) { GL.Uniform3(shaders[activeShader].getUniform("light_color"), ref lights[0].Color); }

                if (shaders[activeShader].getUniform("light_diffuseIntensity") != -1) { GL.Uniform1(shaders[activeShader].getUniform("light_diffuseIntensity"), lights[0].DiffuseIntensity); }

                if (shaders[activeShader].getUniform("light_ambientIntensity") != -1) { GL.Uniform1(shaders[activeShader].getUniform("light_ambientIntensity"), lights[0].AmbientIntensity); }

                //Lighting loop
                for (int i = 0; i < Math.Min(lights.Count, MAX_LIGHTS); i++)
                {
                    if (shaders[activeShader].getUniform($"lights[{i}].position") != -1) { GL.Uniform3(shaders[activeShader].getUniform($"lights[{i}].position"), ref lights[i].Position); }

                    if (shaders[activeShader].getUniform($"lights[{i}].color") != -1) { GL.Uniform3(shaders[activeShader].getUniform($"lights[{i}].color"), ref lights[i].Color); }

                    if (shaders[activeShader].getUniform($"lights[{i}].diffuseIntensity") != -1) { GL.Uniform1(shaders[activeShader].getUniform($"lights[{i}].diffuseIntensity"), lights[i].DiffuseIntensity); }

                    if (shaders[activeShader].getUniform($"lights[{i}].ambientIntensity") != -1) { GL.Uniform1(shaders[activeShader].getUniform($"lights[{i}].ambientIntensity"), lights[i].AmbientIntensity); }

                    if (shaders[activeShader].getUniform($"lights[{i}].direction") != -1) { GL.Uniform3(shaders[activeShader].getUniform($"lights[{i}].direction"), ref lights[i].Direction); }

                    if (shaders[activeShader].getUniform($"lights[{i}].type") != -1) { GL.Uniform1(shaders[activeShader].getUniform($"lights[{i}].type"), (int)lights[i].Type); }

                    if (shaders[activeShader].getUniform($"lights[{i}].coneAngle") != -1) { GL.Uniform1(shaders[activeShader].getUniform($"lights[{i}].coneAngle"), lights[i].ConeAngle); }

                    if (shaders[activeShader].getUniform($"lights[{i}].linearAttenuation") != -1) { GL.Uniform1(shaders[activeShader].getUniform($"lights[{i}].linearAttenuation"), lights[i].LinearAttenuation); }

                    if (shaders[activeShader].getUniform($"lights[{i}].quadraticAttenuation") != -1) { GL.Uniform1(shaders[activeShader].getUniform($"lights[{i}].quadraticAttenuation"), lights[i].QuadraticAttenuation); }
                }

                GL.DrawElements(PrimitiveType.Triangles, v.IndiceCount, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                indiceat += v.IndiceCount;
            }

            shaders[activeShader].disableVertexAttribArrays();

            GL.Flush();

            //Swap which buffer is currently being rendered and which is being displayed
            glCanvas.SwapBuffers();
        }

        private void GlCanvas_Paint(object sender, PaintEventArgs e)
        {
            GlCanvas_Update(0.0f);
            GlCanvas_Render();
        }

        private void GlCanvas_Resize(object sender, EventArgs e)
        {
            glCanvas.MakeCurrent();

            GL.Viewport(0, 0, glCanvas.ClientSize.Width, glCanvas.ClientSize.Height);

            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, Width / (float)Height, 1.0f, 64.0f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);
        }

        private int LoadTexture(Bitmap image)
        {
            int texID = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, texID);

            BitmapData data = image.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            image.UnlockBits(data);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            return texID;
        }

        //Calls loadTexture as an overflow after grabbing the bitmap from the file
        private int LoadTexture(string filename)
        {
            try
            {
                Bitmap file = new Bitmap(filename);
                return LoadTexture(file);
            }
            catch (FileNotFoundException e) { Console.WriteLine($"File not found: {filename}"); return -1; }
            catch (Exception e) { Console.WriteLine($"Error occured while loading texture: {e}"); return -1; }
        }

        //Calls methods from the material class to load materials from a file into gpu memory
        private void LoadMaterials(string filename)
        {
            foreach (var mat in Material.LoadFromFile(filename))
            {
                if (!materials.ContainsKey(mat.Key))
                {
                    materials.Add(mat.Key, mat.Value);
                }
            }

            foreach (Material mat in materials.Values)
            {
                if (File.Exists(mat.AmbientMap) && !textures.ContainsKey(mat.AmbientMap))
                {
                    textures.Add(mat.AmbientMap, LoadTexture(mat.AmbientMap));
                }

                if (File.Exists(mat.DiffuseMap) && !textures.ContainsKey(mat.DiffuseMap))
                {
                    textures.Add(mat.DiffuseMap, LoadTexture(mat.AmbientMap));
                }

                if (File.Exists(mat.SpecularMap) && !textures.ContainsKey(mat.SpecularMap))
                {
                    textures.Add(mat.SpecularMap, LoadTexture(mat.SpecularMap));
                }

                if (File.Exists(mat.NormalMap) && !textures.ContainsKey(mat.NormalMap))
                {
                    textures.Add(mat.NormalMap, LoadTexture(mat.NormalMap));
                }

                if (File.Exists(mat.OpacityMap) && !textures.ContainsKey(mat.OpacityMap))
                {
                    textures.Add(mat.OpacityMap, LoadTexture(mat.OpacityMap));
                }
            }
        }

        //Loads all resouces into main memory and gpu memory
        private void LoadResources()
        {
            //Load shaders
            shaders.Add("default", new ShaderProgram("resources/shaders/vs.glsl", "resources/shaders/fs.glsl", true));
            shaders.Add("textured", new ShaderProgram("resources/shaders/vs_tex.glsl", "resources/shaders/fs_tex.glsl", true));
            shaders.Add("lit", new ShaderProgram("resources/shaders/vs_lit.glsl", "resources/shaders/fs_lit_advanced.glsl", true));

            //Load materials (.mtl)
            LoadMaterials("resources/materials/opentk.mtl");

            //Load textures (.png, .bmp, .jpg)
            textures.Add("dev_measurewall01a", LoadTexture("resources/textures/dev_measurewall01a.png"));

            //Load models (.obj)
        }

        //Creates all default objects and moves camera to initial position
        private void SetupScene()
        {
            cam.Position += new Vector3(0.0f, 0.0f, 3.0f);
        }

        private void vSyncToolStripMenuItem_Click(object sender, EventArgs e)
        {
            glCanvas.VSync = !glCanvas.VSync;
            vSyncToolStripMenuItem.Checked = glCanvas.VSync;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFile = new OpenFileDialog())
            {
                openFile.FileName = ""; openFile.Filter = "VMF Files (*.vmf)|*.vmf"; openFile.Title = "Mjolnir Editor - Open File";
                openFile.RestoreDirectory = true;

                if (openFile.ShowDialog() == DialogResult.OK)
                {
                    if (currentFile != null)
                    {
                        DialogResult result = MessageBox.Show(
                            "Opening another file will overwrite the currently open file, do you wish to continue?", 
                            "Mjolnir Editor",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning
                        );

                        if (result == DialogResult.Yes)
                        {
                            world = new World();
                            loadVMF(openFile.FileName, ref world);
                            currentFile = openFile.FileName;
                            
                            
                        }
                    }
                    else
                    {
                        world = new World();
                        loadVMF(openFile.FileName, ref world);
                        currentFile = openFile.FileName;
                    }

                    foreach (Solid brush in world.brushes)
                    {
                        foreach (Face face in brush.sides)
                        {
                            Vector3[] verticies = new Vector3[4];

                            for (int i = 0; i < face.verticies.Count; i++)
                            {
                                //verticies[i] = face.verticies[i] * new Vector3() {X=0.02f, Y=0.02f, Z=0.02f};
                                verticies[i] = new Vector3(
                                    face.verticies[i].X * 0.02f,
                                    face.verticies[i].Z * 0.02f,
                                    face.verticies[i].Y * 0.02f
                                );
                            }
                            
                            TexturedPlane newPlane = new TexturedPlane(verticies);
                            newPlane.Material = materials["opentk1"];
                            //newPlane.TextureID = textures["dev_grey.png"];
                            newPlane.TextureID = textures["dev_measurewall01a"];

                            objects.Add(newPlane);
                        }
                    }

                    foreach (Light light in world.lights)
                    {
                        lights.Add(light);
                    }
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Save();
        }
        
        static public void addKeyValuePairs(List<string> lines, ref Dictionary<string, int> dict)
        {
            foreach (string line in lines)
            {
                dict.Add(line.Split(' ')[0].Trim('"'), Convert.ToInt32(line.Split(' ')[1].Trim('"')));
            }
        }
        
        static public void addKeyValuePairs(List<string> lines, ref Dictionary<string, string> dict)
        {
            foreach (string line in lines)
            {
                dict.Add(line.Split(' ')[0].Trim('"'), line.Split(' ')[1].Trim('"'));
            }
        }

        static public string getTabs(int numOfTabs)
        {
            string tabs = "";

            for (int i = 0; i < numOfTabs; i++)
            {
                tabs += "   ";
            }

            return tabs;
        }
        
        static public int loadVMF(string filename, ref World world)
        {
            int lineno = 1;

            try
            {
                using (StreamReader sr = new StreamReader(filename))
                {
                    string nextline = null;
                    
                    while (!sr.EndOfStream)
                    {
                        if (nextline != "solid")
                        {
                            lineno++;
                            nextline = sr.ReadLine().TrimStart();
                        }
                            
                        if (nextline == "versioninfo")
                        {
                            #if DEBUG
                            Console.WriteLine("Found version info");
                            #endif

                            lineno++;
                            sr.ReadLine(); //Skip the opening bracket
                            lineno++;
                            nextline = sr.ReadLine().TrimStart();
                            List<string> lines = new List<string>();

                            while (nextline != "}")
                            {
                                lines.Add(nextline);
                                lineno++;
                                nextline = sr.ReadLine().TrimStart();
                            }

                            addKeyValuePairs(lines, ref world.versionInfo);
                        }
                        else if (nextline == "viewsettings")
                        {
                            #if DEBUG
                            Console.WriteLine("Found view settings");
                            #endif

                            lineno++;
                            sr.ReadLine();
                            lineno++;
                            nextline = sr.ReadLine().TrimStart();
                            List<string> lines = new List<string>();

                            while (nextline != "}")
                            {
                                lines.Add(nextline);
                                lineno++;
                                nextline = sr.ReadLine().TrimStart();
                            }

                            addKeyValuePairs(lines, ref world.viewSettings);
                        }
                        else if (nextline == "world")
                        {
                            #if DEBUG
                            Console.WriteLine("Found world ent");
                            #endif

                            lineno++;
                            sr.ReadLine();
                            lineno++;
                            nextline = sr.ReadLine().TrimStart();
                            List<string> lines = new List<string>();

                            while (nextline != "}" && nextline != "solid")
                            {
                                lines.Add(nextline);
                                lineno++;
                                nextline = sr.ReadLine().TrimStart();
                            }

                            addKeyValuePairs(lines, ref world.entValues);
                        }
                        else if (nextline == "solid")
                        {
                            #if DEBUG
                            Console.WriteLine("Found brush entity");
                            #endif

                            lineno++;
                            sr.ReadLine();
                            lineno++;
                            string idString = sr.ReadLine().TrimStart();
                            lineno++;
                            nextline = sr.ReadLine().TrimStart();
                            lineno++;

                            List<Face> brushSides = new List<Face>();
                            bool brushFinished = false;

                            while (!brushFinished)
                            {
                                if (nextline == "side")
                                {
                                    lineno++;
                                    sr.ReadLine();
                                    lineno++;
                                    string nextsubstring = sr.ReadLine().TrimStart();
                                    Face face = new Face();

                                    while (nextsubstring != "}")
                                    {
                                        if (nextsubstring.StartsWith("\"id\""))
                                        {
                                            face.id = Convert.ToInt32(nextsubstring.Split(' ')[1].Trim('"'));
                                        }
                                        else if (nextsubstring.StartsWith("\"material\""))
                                        {
                                            face.material = nextsubstring.Split(' ')[1].Trim('"');
                                        }
                                        else if (nextsubstring.StartsWith("\"uaxis\""))
                                        {
                                            face.uaxis = nextsubstring.Split(new string[] {"\" \""}, StringSplitOptions.None)[1].Trim('"');
                                        }
                                        else if (nextsubstring.StartsWith("\"vaxis\""))
                                        {
                                            face.vaxis = nextsubstring.Split(new string[] {"\" \""}, StringSplitOptions.None)[1].Trim('"');
                                        }
                                        else if (nextsubstring.StartsWith("\"rotation\""))
                                        {
                                            face.rotation = Convert.ToInt32(nextsubstring.Split(' ')[1].Trim('"'));
                                        }
                                        else if (nextsubstring.StartsWith("\"lightmapscale\""))
                                        {
                                            face.lightmapScale = Convert.ToInt32(nextsubstring.Split(' ')[1].Trim('"'));
                                        }
                                        else if (nextsubstring.StartsWith("\"smoothing_groups\""))
                                        {
                                            face.smoothingGroups =
                                                Convert.ToInt32(nextsubstring.Split(' ')[1].Trim('"'));
                                        }
                                        else if (nextsubstring.StartsWith("\"plane\""))
                                        {
                                            string unsplitCoords = nextsubstring.Split(new string[] {"\" \""}, StringSplitOptions.None)[1].Trim('"').Trim();

                                            List<Vector3> verticies = new List<Vector3>();

                                            foreach (string vertex in unsplitCoords.Split(new string[] {") ("}, StringSplitOptions.None))
                                            {
                                                string[] coords = vertex.Trim('(').Trim(')').Trim().Split(' ');
                                                verticies.Add(new Vector3(float.Parse(coords[0].Trim()),
                                                    float.Parse(coords[1].Trim()), float.Parse(coords[2].Trim())));
                                            }

                                            if (verticies.Count == 3)
                                            {
                                                Vector3 finalVertex = Vector3.Zero;

                                                if (verticies[1].X >= 0 && verticies[2].X >= 0)
                                                {
                                                    finalVertex.X = verticies[0].X + verticies[2].X - verticies[1].X;
                                                } else if (verticies[1].X >= 0 && verticies[2].X < 0)
                                                {
                                                    finalVertex.X = verticies[0].X + verticies[2].X - verticies[1].X;
                                                } else if (verticies[1].X < 0 && verticies[2].X >= 0)
                                                {
                                                    finalVertex.X = verticies[0].X - verticies[2].X - verticies[1].X;
                                                } else if (verticies[1].X < 0 && verticies[2].X < 0)
                                                {
                                                    finalVertex.X = verticies[0].X - verticies[2].X + verticies[1].X;
                                                } else// if (verticies[1].X < 0)
                                                {
                                                    finalVertex.X = verticies[0].X + verticies[2].X - verticies[1].X;
                                                }

                                                if (verticies[1].Y >= 0 && verticies[2].Y >= 0)
                                                {
                                                    finalVertex.Y = verticies[0].Y + verticies[2].Y - verticies[1].Y;
                                                } else if (verticies[1].Y >= 0 && verticies[2].Y < 0)
                                                {
                                                    finalVertex.Y = verticies[0].Y + verticies[2].Y - verticies[1].Y;
                                                } else if (verticies[1].Y < 0 && verticies[2].Y >= 0)
                                                {
                                                    finalVertex.Y = verticies[0].Y - verticies[2].Y - verticies[1].Y;
                                                } else if (verticies[1].Y < 0 && verticies[2].Y < 0)
                                                {
                                                    finalVertex.Y = verticies[0].Y - verticies[2].Y + verticies[1].Y;
                                                } else// if (verticies[1].Y < 0)
                                                {
                                                    finalVertex.Y = verticies[0].Y + verticies[2].Y - verticies[1].Y;
                                                }
                                                
                                                if (verticies[1].Z >= 0 && verticies[2].Z >= 0)
                                                {
                                                    finalVertex.Z = verticies[0].Z + verticies[2].Z - verticies[1].Z;
                                                } else if (verticies[1].Z >= 0 && verticies[2].Z < 0)
                                                {
                                                    finalVertex.Z = verticies[0].Z + verticies[2].Z - verticies[1].Z;
                                                } else if (verticies[1].X < 0 && verticies[2].X >= 0)
                                                {
                                                    finalVertex.Z = verticies[0].Z - verticies[2].Z - verticies[1].Z;
                                                } else if (verticies[1].Z < 0 && verticies[2].Z < 0)
                                                {
                                                    finalVertex.Z = verticies[0].Z - verticies[2].Z + verticies[1].Z;
                                                } else// if (verticies[1].Z < 0)
                                                {
                                                    finalVertex.Z = verticies[0].Z + verticies[2].Z - verticies[1].Z;
                                                }
                                                
                                                //Vector3 finalVertex = new Vector3(
                                                //    verticies[0].X + verticies[2].X - verticies[1].X,
                                                //    verticies[0].Y + verticies[2].Y - verticies[1].Y,
                                                //    verticies[0].Z + verticies[2].Z - verticies[2].Z
                                                //);


                                                verticies.Add(finalVertex);
                                            }

                                            face.verticies = verticies;
                                        }

                                        lineno++;
                                        nextsubstring = sr.ReadLine().TrimStart();
                                    }

                                    brushSides.Add(face);
                                }
                                else if (nextline == "}")
                                {
                                    brushFinished = true;
                                }

                                lineno++;
                                nextline = sr.ReadLine().TrimStart();
                            }

                            world.brushes.Add(new Solid()
                            {
                                id = Convert.ToInt32(idString.Split(' ')[1].Trim('"')),
                                sides = brushSides
                            });
                        }
                        else if (nextline == "cameras")
                        {
                            #if DEBUG
                            Console.WriteLine("Found cameras");
                            #endif

                            lineno++;
                            sr.ReadLine();
                            lineno++;
                            nextline = sr.ReadLine().TrimStart();

                            if (nextline.StartsWith("\"activecamera\""))
                            {
                                world.activeCamera = int.Parse(nextline.Split(' ')[1].Trim('"'));
                            }
                        }
                        else if (nextline == "cordons")
                        {
                            #if DEBUG
                            Console.WriteLine("Found cordons");
                            #endif

                            lineno++;
                            sr.ReadLine();
                            lineno++;
                            nextline = sr.ReadLine().TrimStart();

                            if (nextline.StartsWith("\"active\""))
                            {
                                world.activeCordon = int.Parse(nextline.Split(' ')[1].Trim('"'));
                            }
                        }
                        else if (nextline == "entity")
                        {
                            #if DEBUG
                            Console.WriteLine("Found entity");
                            #endif
                            
                            lineno++;
                            sr.ReadLine();
                            lineno++;
                            string idString = sr.ReadLine().TrimStart();
                            lineno++;
                            nextline = sr.ReadLine().TrimStart();

                            if (nextline.StartsWith("\"classname\""))
                            {
                                Dictionary<string, string> entDictionary = new Dictionary<string, string>();
                                string className = nextline.Split(' ')[1].Trim('"');

                                lineno++;
                                nextline = sr.ReadLine().TrimStart();

                                while (nextline != "}" && nextline != "editor")
                                {
                                    entDictionary.Add(nextline.Split(new string[] {"\" \""}, StringSplitOptions.None)[0].Trim('"'),
                                        nextline.Split(new string[] {"\" \""}, StringSplitOptions.None)[1].Trim('"'));

                                    lineno++;
                                    nextline = sr.ReadLine().TrimStart();
                                }

                                if (className == "light_environment")
                                {
                                    Light newLight = new Light(
                                        int.Parse(idString.Split(' ')[1].Trim('"')),
                                        new Vector3(float.Parse(entDictionary["origin"].Split(' ')[0].Trim('"')),
                                            float.Parse(entDictionary["origin"].Split(' ')[1].Trim('"')),
                                            float.Parse(entDictionary["origin"].Split(' ')[2].Trim('"'))),
                                        new Vector3(float.Parse(entDictionary["_light"].Split(' ')[0].Trim('"')) / 255f,
                                            float.Parse(entDictionary["_light"].Split(' ')[1].Trim('"')) / 255f,
                                            float.Parse(entDictionary["_light"].Split(' ')[2].Trim('"')) / 255f),
                                        float.Parse(entDictionary["_light"].Split(' ')[3].Trim('"')) / 5f,
                                        float.Parse(entDictionary["_ambient"].Split(' ')[3].Trim('"')) / 5f
                                    );
                                    
                                    newLight.Type = LightType.Directional;
                                    newLight.Direction = new Vector3(
                                        float.Parse(entDictionary["angles"].Split(' ')[0].Trim('"')),
                                        float.Parse(entDictionary["angles"].Split(' ')[1].Trim('"')),
                                        float.Parse(entDictionary["angles"].Split(' ')[2].Trim('"'))
                                    );

                                    world.lights.Add(newLight);
                                }
                                else if (className == "light")
                                {
                                    Light newLight = new Light(
                                        int.Parse(idString.Split(' ')[1].Trim('"')),
                                        new Vector3(float.Parse(entDictionary["origin"].Split(' ')[0].Trim('"')),
                                            float.Parse(entDictionary["origin"].Split(' ')[1].Trim('"')),
                                            float.Parse(entDictionary["origin"].Split(' ')[2].Trim('"'))),
                                        new Vector3(float.Parse(entDictionary["_light"].Split(' ')[0].Trim('"')) / 255f,
                                            float.Parse(entDictionary["_light"].Split(' ')[1].Trim('"')) / 255f,
                                            float.Parse(entDictionary["_light"].Split(' ')[2].Trim('"')) / 255f),
                                        float.Parse(entDictionary["_light"].Split(' ')[3].Trim('"')) / 5f
                                        /*float.Parse(entDictionary["_ambient"].Split(' ')[3].Trim('"')) / 255f*/
                                    );

                                    newLight.Type = LightType.Point;

                                    if (entDictionary["_linear_attn"].Trim('"') == "1")
                                    {
                                        newLight.LinearAttenuation = 0.025f;
                                    }
                                    else if (entDictionary["_quadratic_attn"].Trim('"') == "1")
                                    {
                                        newLight.LinearAttenuation = 0.025f;
                                    }

                                    world.lights.Add(newLight);
                                }
                            }
                        }
                    }
                }

                return 1;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error reading file occured at line {lineno}:");
                Console.WriteLine(e);
                return 0;
            }
        }

        static public int saveVMF(string filename, ref World world)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(filename))
                {
                    int depth = 0;
                    sw.WriteLine(getTabs(depth) + "versioninfo");
                    sw.WriteLine(getTabs(depth) + "{");
                    depth++;

                    foreach (KeyValuePair<string, int> keyValuePair in world.versionInfo)
                    {
                        sw.WriteLine($"{getTabs(depth)}\"{keyValuePair.Key}\" \"{keyValuePair.Value}\"");
                    }

                    depth--;
                    sw.WriteLine(getTabs(depth) + "}");

                    sw.WriteLine(getTabs(depth) + "viewsettings");
                    sw.WriteLine(getTabs(depth) + "{");
                    depth++;

                    foreach (KeyValuePair<string, int> keyValuePair in world.viewSettings)
                    {
                        sw.WriteLine($"{getTabs(depth)}\"{keyValuePair.Key}\" \"{keyValuePair.Value}\"");
                    }

                    depth--;
                    sw.WriteLine(getTabs(depth) + "}");

                    sw.WriteLine("world");
                    sw.WriteLine(getTabs(depth) + "{");
                    depth++;

                    foreach (KeyValuePair<string, string> keyValuePair in world.entValues)
                    {
                        sw.WriteLine($"{getTabs(depth)}\"{keyValuePair.Key}\" \"{keyValuePair.Value}\"");
                    }

                    foreach (Solid solid in world.brushes)
                    {
                        sw.WriteLine(getTabs(depth) + "solid");
                        sw.WriteLine(getTabs(depth) + "{");
                        depth++;
                        sw.WriteLine($"{getTabs(depth)}\"id\" \"{solid.id}\"");

                        foreach (Face face in solid.sides)
                        {
                            sw.WriteLine(getTabs(depth) + "side");
                            sw.WriteLine(getTabs(depth) + "{");
                            depth++;
                            
                            sw.WriteLine($"{getTabs(depth)}\"id\" \"{face.id}\"");
                            sw.WriteLine($"{getTabs(depth)}\"plane\" \"({face.verticies[0].X} " +
                                         $"{face.verticies[0].Y} " +
                                         $"{face.verticies[0].Z}) (" +
                                         $"{face.verticies[1].X} " +
                                         $"{face.verticies[1].Y} " +
                                         $"{face.verticies[1].Z}) (" +
                                         $"{face.verticies[2].X} " +
                                         $"{face.verticies[2].Y} " +
                                         $"{face.verticies[2].Z})\"");
                            sw.WriteLine($"{getTabs(depth)}\"material\" \"{face.material}\"");
                            sw.WriteLine($"{getTabs(depth)}\"uaxis\" \"{face.uaxis}\"");
                            sw.WriteLine($"{getTabs(depth)}\"vaxis\" \"{face.vaxis}\"");
                            sw.WriteLine($"{getTabs(depth)}\"rotation\" \"{face.rotation}\"");
                            sw.WriteLine($"{getTabs(depth)}\"lightmapscale\" \"{face.lightmapScale}\"");
                            sw.WriteLine($"{getTabs(depth)}\"smoothing_groups\" \"{face.smoothingGroups}\"");

                            depth--;
                            sw.WriteLine(getTabs(depth) + "}");
                        }
                        
                        depth--;
                        sw.WriteLine(getTabs(depth) + "}");
                    }

                    depth--;
                    sw.WriteLine(getTabs(depth) + "}");

                    foreach (Entity entity in world.entities)
                    {
                        sw.WriteLine(getTabs(depth) + "entity");
                        sw.WriteLine(getTabs(depth) + "{");
                        depth++;
                        
                        sw.WriteLine($"{getTabs(depth)}\"id\" \"{entity.id}\"");

                        foreach (KeyValuePair<string, string> keyValuePair in entity.entSettings)
                        {
                            sw.WriteLine($"{getTabs(depth)}\"{keyValuePair.Key}\" \"{keyValuePair.Value}\"");
                        }

                        depth--;
                        sw.WriteLine(getTabs(depth) + "}");
                    }

                    foreach (Light light in world.lights)
                    {
                        sw.WriteLine(getTabs(depth) + "entity");
                        sw.WriteLine(getTabs(depth) + "{");
                        depth++;
                        
                        sw.WriteLine($"{getTabs(depth)}\"id\" \"{light.id}\"");

                        if (light.Type == LightType.Directional)
                        {
                            sw.WriteLine($"{getTabs(depth)}\"classname\" \"light_environment\"");
                            sw.WriteLine($"{getTabs(depth)}\"_ambient\" \"" +
                                         $"255 255 255 {light.AmbientIntensity * 255f}\"");
                            sw.WriteLine(getTabs(depth) + "\"_ambientHDR\" \"-1 -1 -1 1\"");
                            sw.WriteLine(getTabs(depth) + "\"_AmbientScaleHDR\" \"1\"");
                            sw.WriteLine($"{getTabs(depth)}\"_light\" \"" +
                                         $"{light.Color.X * 255f} " +
                                         $"{light.Color.Y * 255f} " +
                                         $"{light.Color.Z * 255f} " +
                                         $"{light.DiffuseIntensity}\"");
                            sw.WriteLine(getTabs(depth) + "\"_lightHDR\" \"-1 -1 -1 1\"");
                            sw.WriteLine(getTabs(depth) + "\"_lightscaleHDR\" \"1\"");
                            sw.WriteLine($"{getTabs(depth)}\"angles\" \"" +
                                         $"{light.Direction.X} " +
                                         $"{light.Direction.Y} " +
                                         $"{light.Direction.Z}\"");
                            sw.WriteLine($"{getTabs(depth)}\"pitch\" \"{light.Direction.X}\"");
                            sw.WriteLine(getTabs(depth) + "\"SunSpreadAngle\" \"0\"");
                            sw.WriteLine($"{getTabs(depth)}\"origin\" \"" +
                                         $"{light.Position.X} " +
                                         $"{light.Position.Y} " +
                                         $"{light.Position.Z}\"");
                        }
                        else if (light.Type == LightType.Point)
                        {
                            sw.WriteLine(getTabs(depth) + "\"classname\" \"light\"");
                            sw.WriteLine($"{getTabs(depth)}\"_constant_attn\" \"0\"");
                            sw.WriteLine($"{getTabs(depth)}\"_distance\" \"0\"");
                            sw.WriteLine($"{getTabs(depth)}\"_fifty_percent_distance\" \"0\"");
                            sw.WriteLine($"{getTabs(depth)}\"_hardfalloff\" \"0\"");
                            sw.WriteLine($"{getTabs(depth)}\"_light\" \"" +
                                         $"{light.Color.X} " +
                                         $"{light.Color.Y} " +
                                         $"{light.Color.Z}\"");
                            sw.WriteLine($"{getTabs(depth)}\"_lightHDR\" \"-1 -1 -1 1\"");
                            sw.WriteLine($"{getTabs(depth)}\"_lightScaleHDR\" \"1\"");
                            string linearAtten = light.LinearAttenuation > 0 ? "1" : "0";
                            sw.WriteLine($"{getTabs(depth)}\"_linear_attn\" \"{linearAtten}\"");
                            string quadraticAtten = light.QuadraticAttenuation > 0 ? "1" : "0";
                            sw.WriteLine($"{getTabs(depth)}\"_quadratic_attn\" \"{quadraticAtten}\"");
                            sw.WriteLine($"{getTabs(depth)}\"_zero_percent_distance\" \"0\"");
                            sw.WriteLine($"{getTabs(depth)}\"spawnflags\" \"0\""); 
                            sw.WriteLine($"{getTabs(depth)}\"style\" \"0\"");
                            sw.WriteLine($"{getTabs(depth)}\"origin\" \"" +
                                         $"{light.Position.X} " +
                                         $"{light.Position.Y} " +
                                         $"{light.Position.Z}\"");
                        }

                        depth--;
                        sw.WriteLine(getTabs(depth) + "}");
                    }
                    
                    sw.WriteLine(getTabs(depth) + "cameras");
                    sw.WriteLine(getTabs(depth) + "{");
                    depth++;
                    
                    sw.WriteLine($"{getTabs(depth)}\"activecamera\" \"{world.activeCamera}\"");

                    depth--;
                    sw.WriteLine(getTabs(depth) + "}");

                    sw.WriteLine(getTabs(depth) + "cordons");
                    sw.WriteLine(getTabs(depth) + "{");
                    depth++;

                    sw.WriteLine($"{getTabs(depth)}\"active\" \"{world.activeCordon}\"");

                    depth--;
                    sw.WriteLine(getTabs(depth) + "}");
                }

                return 1;
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occured while writing file:\n{e}");
                return 0;
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFile = new SaveFileDialog())
            {
                saveFile.FileName = "";
                saveFile.Filter = "VMF Files (*.vmf)|*.vmf";
                saveFile.Title = "Mjolnir Editor - Save File";
                saveFile.RestoreDirectory = true;

                if (saveFile.ShowDialog() == DialogResult.OK)
                {
                    currentFile = saveFile.FileName;
                    saveVMF(saveFile.FileName, ref world);
                }
            }
        }

        private void saveToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (currentFile != null)
            {
                saveVMF(currentFile, ref world);
            }
            else
            {
                saveToolStripMenuItem_Click(sender, e);
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == (int) Keys.Space)
            {
                controlCamera = !controlCamera;

                if (controlCamera) { Cursor.Hide(); } 
                else { Cursor.Show(); }
            }
        }
    }
}
