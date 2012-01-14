using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MBHEngineContentDefs
{
    public class OgmoRenderDefinition : BehaviourDefinition
    {
        public class LayerInfo
        {
            public String mName;
            public Boolean mUsesTextureIndex;
        };
        /// <summary>
        /// Path and Name of Ogmo level file to load (*.oel).
        /// </summary>
        public String mOgmoLevel;

        public String mGridLayer;

        public List<LayerInfo> mTileLayers;
    }
}
