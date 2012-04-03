﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace MBHEngineContentDefs
{
    public class GameObjectDefinition
    {
        /// <summary>
        /// Determines the order at which IGameObjects should be rendered.
        /// The higher the number the later it will be rendered.
        /// </summary>
        public Int32 mRenderPriority;

        /// <summary>
        /// Whether on not this object should be updated.
        /// </summary>
        public Boolean mDoUpdate;

        /// <summary>
        /// Whether on not this object should be rendered.
        /// </summary>
        public Boolean mDoRender;

        /// <summary>
        /// The position of the object in space.
        /// </summary>
        public Vector2 mPosition;

        /// <summary>
        /// The rotation of the object around the Z axis.
        /// </summary>
        public Single mRotation;

        /// <summary>
        /// The scale of the object in both the x and y axis independently.
        /// </summary>
        public Vector2 mScale;

        /// <summary>
        /// The calssifications for this game object.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public List<Int32> mClassifications;

        /// <summary>
        /// The different types of blending that a gameobject can use when rendering.
        /// </summary>
        public enum BlendMode
        {
            UNDEFINED = 0,
            STANDARD = 1,
            MULTIPLY = 2,
            MULTIPLY_UI = 3,
        };

        /// <summary>
        /// The type of blending to be used when rendering this object.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public BlendMode mBlendMode = BlendMode.STANDARD;

        /// <summary>
        /// A list of all the behaviours this game object has.  These are indentified by
        /// the name of the definition file for each.
        /// </summary>
        public List<String> mBehaviourFileNames;

        /// <summary>
        /// This should be an index to index mapping with mBehaviorFileNames, where instead
        /// of defining the file names, we are defining the class names of the behaviour.
        /// This needs to be the full class and names space.  For example:
        /// MBHEngine.Code.Behaviour.Behaviour
        /// </summary>
        public List<String> mBehaviourClassNames;
    }
}
