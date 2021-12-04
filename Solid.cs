using System.Collections.Generic;
using OpenTK;

namespace mjolnir_editor
{
    public class Solid
    {
        public int id;
        public List<Face> sides;
    }

    public class Face
    {
        public int id;
        public List<Vector3> verticies = new List<Vector3>();
        public string material;
        public string uaxis;
        public string vaxis;
        public int rotation;
        public int lightmapScale;
        public int smoothingGroups;
    }
}