using System.Collections.Generic;

namespace mjolnir_editor
{
    public class World
    {
        public Dictionary<string, int> versionInfo = new Dictionary<string, int>();
        public Dictionary<string, int> viewSettings = new Dictionary<string, int>();
        public Dictionary<string, string> entValues = new Dictionary<string, string>();
        public List<Solid> brushes = new List<Solid>();
        public List<Light> lights = new List<Light>();
        public List<Entity> entities = new List<Entity>();
        public int activeCamera = -1;
        public int activeCordon = 0;
    }
}