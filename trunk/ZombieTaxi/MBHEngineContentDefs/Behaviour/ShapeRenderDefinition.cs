using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;

namespace MBHEngineContentDefs
{
    public class ShapeRenderDefinition : BehaviourDefinition
    {
        public class Shape
        {
            public Point mDimensions;

            public Color mColor;
        }

        public List<Shape> mShapes;
    }
}
