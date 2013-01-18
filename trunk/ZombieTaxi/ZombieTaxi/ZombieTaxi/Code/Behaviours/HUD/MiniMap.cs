using System;
using System.Collections.Generic;
using MBHEngine.Behaviour;
using MBHEngine.GameObject;
using MBHEngine.World;
using MBHEngineContentDefs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ZombieTaxiContentDefs;

namespace ZombieTaxi.Behaviours.HUD
{
    /// <summary>
    /// HUD element which draws a little box on the screen with pixels on it to represent different objects in the 
    /// world.
    /// </summary>
    class MiniMap : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Tell the MiniMap an Object that should be marked.
        /// </summary>
        public class MarkObjectMessage : BehaviourMessage
        {
            /// <summary>
            /// The MarkerProfileDefinition to use when marking this object.
            /// </summary>
            public String mMarkerProfile;

            /// <summary>
            /// The GameObject to mark. It will dynamically following the object.
            /// </summary>
            public GameObject mObjectToMark;
        }

        /// <summary>
        /// Contains all the information for a single Marker on the MiniMap.
        /// </summary>
        public struct Marker
        {
            /// <summary>
            /// Tells the Marker how to render.
            /// </summary>
            private MarkerProfile mProfile;

            /// <summary>
            /// The object that is being marked.
            /// </summary>
            private GameObject mObjectToMark;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="fileName">MarkerProfileDefinition file.</param>
            /// <param name="objectToMark">A GameObject to mark.</param>
            public Marker(String fileName, GameObject objectToMark) 
            {
                /// <todo> 
                /// This should use some sort of factory. No need to load these profiles
                /// over and over again, especially in the middle of the game. 
                /// </todo>
                mProfile = GameObjectManager.pInstance.pContentManager.Load<MarkerProfile>(fileName);

                mObjectToMark = objectToMark;
            }

            /// <summary>
            /// 
            /// </summary>
            public GameObject pObjectToMark
            {
                get
                {
                    return mObjectToMark;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public MarkerProfile pProfile
            {
                get
                {
                    return mProfile;
                }
            }
        }

        /// <summary>
        /// The minimap is just a proceedurally created texture which we directly draw 
        /// pixels to.
        /// </summary>
        private Texture2D mMapTexture;

        /// <summary>
        /// To write to mMapTexture we send in an array of Color which gets written in 
        /// order to each pixel.
        /// </summary>
        private Color[] mColorData;

        /// <summary>
        /// Convert from tile index to pixel index on mMapTexture.
        /// </summary>
        private Vector2 mTileToMapScale;

        /// <summary>
        /// Convert from World Space to pixel index on mMapTexture.
        /// </summary>
        private Vector2 mWorldToMapScale;

        /// <summary>
        /// The width of the minimap in pixels.
        /// </summary>
        private Point mMiniMapSize;
        
        /// <summary>
        /// Preallocate messages to avoid GC.
        /// </summary>
        private Level.GetMapInfoMessage mGetMapInfoMsg;

        /// <summary>
        /// A list of all the Markers on this MiniMap.
        /// </summary>
        private List<Marker> mMarkers;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public MiniMap(GameObject parentGOH, String fileName)
            : base(parentGOH, fileName)
        {
        }

        /// <summary>
        /// Call this to initialize a Behaviour with data supplied in a file.
        /// </summary>
        /// <param name="fileName">The file to load from.</param>
        public override void LoadContent(String fileName)
        {
            base.LoadContent(fileName);

            //ExampleDefinition def = GameObjectManager.pInstance.pContentManager.Load<ExampleDefinition>(fileName);

            // We depend on the Map being loaded so that we can use the size of it to drive the size of the MiniMap.
            mGetMapInfoMsg = new Level.GetMapInfoMessage();
            WorldManager.pInstance.pCurrentLevel.OnMessage(mGetMapInfoMsg);

            // For now just a hard coded scale from tile position to minimap pixel.
            mTileToMapScale = new Vector2(0.1f, 0.1f);

            // Since tile width drives the size of the map, it also drives how
            // we convert from world position to pixel index on the map.
            mWorldToMapScale = new Vector2(mTileToMapScale.X / mGetMapInfoMsg.mInfo.mTileWidth, mTileToMapScale.Y / mGetMapInfoMsg.mInfo.mTileHeight);

            // The map dimensions are used throughout this class.
            Int32 mapWidth = (Int32)((Single)mGetMapInfoMsg.mInfo.mMapWidth * mTileToMapScale.X);
            Int32 mapHeight = (Int32)((Single)mGetMapInfoMsg.mInfo.mMapHeight * mTileToMapScale.Y);
            mMiniMapSize = new Point(mapWidth, mapHeight);

            // Create a new texture to use as our MiniMap.
            mMapTexture = new Texture2D(GameObjectManager.pInstance.pGraphicsDevice, mMiniMapSize.X, mMiniMapSize.Y);

            // This array is how data will be written to the MiniMap.
            mColorData = new Color[mMiniMapSize.X * mMiniMapSize.Y];

            Int32 safeZonePadding = 5;
            Int32 rightEdge = 160;
            Int32 bottomEdge = 90;

            mParentGOH.pPosX = rightEdge - mMiniMapSize.X - safeZonePadding;
            mParentGOH.pPosY = bottomEdge - mMiniMapSize.Y - safeZonePadding;

            mMarkers = new List<Marker>(32);
        }

        /// <summary>
        /// Clears all color data to default values. Should be done at the start of every update.
        /// </summary>
        private void ClearColorData()
        {
            for (Int32 i = 0; i < mColorData.Length; ++i)
            {
                mColorData[i] = Color.Black;
                mColorData[i].A = 100;
            }
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            Vector2 mapPos;
            Int32 index;

            // Start with a blank map.
            ClearColorData();

            // Go through every Marker and render it to the mColorData Array.
            for (Int32 i = 0; i < mMarkers.Count; i++)
            {
                GameObject obj = mMarkers[i].pObjectToMark;

                // Covert the position into a pixel index on the map.
                mapPos = obj.pPosition * mWorldToMapScale;

                // Map X,Y into 1D array of Color.
                index = MBHEngine.Math.Util.Map2DTo1DArray(mMiniMapSize.X, mapPos);

                // Just in case the object gets placed outside the valid map area.
                if (index >= 0 && index < mColorData.Length)
                {
                    mColorData[index] = mMarkers[i].pProfile.mColor;
                }            
            }

            // Sometimes the game will throw an exception because we are trying to write to a texture which
            // is locked by the graphic device. This frees it up apparently.
            //GameObjectManager.pInstance.pGraphicsDevice.Textures[0] = null;

            // Take all that color data an write it to the texture.
            mMapTexture.SetData(mColorData);
        }

        /// <summary>
        /// Called once render cycle by the game object manager.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        /// <param name="effect">The currently set shader.</param>
        public override void Render(SpriteBatch batch, Effect effect)
        {
            batch.Draw(
                mMapTexture,
                new Vector2(mParentGOH.pPosition.X + 1, mParentGOH.pPosition.Y + 1),
                Color.White);
        }

        /// <summary>
        /// The main interface for communicating between behaviours.  Using polymorphism, we
        /// define a bunch of different messages deriving from BehaviourMessage.  Each behaviour
        /// can then check for particular upcasted messahe types, and either grab some data 
        /// from it (set message) or store some data in it (get message).
        /// </summary>
        /// <param name="msg">The message being communicated to the behaviour.</param>
        public override void OnMessage(ref BehaviourMessage msg)
        {
            if (msg is MarkObjectMessage)
            {
                MarkObjectMessage tmp = (MarkObjectMessage)msg;

                MarkObjects(tmp.mMarkerProfile, tmp.mObjectToMark);
            }
        }

        /// <summary>
        /// Adds a new Marker to the MiniMap.
        /// </summary>
        /// <param name="markerProfile">MarkerProfileDefinition file.</param>
        /// <param name="objectToMark">The GameObject being marked.</param>
        private void MarkObjects(String markerProfile, GameObject objectToMark)
        {
            mMarkers.Add(new Marker(markerProfile, objectToMark));
        }
    }
}
