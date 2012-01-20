using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;
using MBHEngineContentDefs;
using Box2D.XNA;
using MBHEngine.Math;

namespace MBHEngine.Behaviour
{
    /// <summary>
    /// Handles the rendering of a tile map.
    /// </summary>
    class TileMapRender : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Wraps up all the data pertaining to Tiles.
        /// </summary>
        private class Tile
        {
        };

        /// <summary>
        /// The texture used to render the sprite.
        /// </summary>
        public Texture2D mTexture;

        /// <summary>
        /// Stores all the information about each tile in the level.
        /// </summary>
        private List<Int32> mMapData;

        /// <summary>
        /// The width of each tile.  Used to calculate the source rectangle when rendering.
        /// </summary>
        private Int32 mSourceWidth;

        /// <summary>
        /// The height of each tile.  Used to calculate the source rectangle when rendering.
        /// </summary>
        private Int32 mSourceHeight;

        private Int32 mMapCountWidth;

        private Int32 mMapCountHeight;

        Body mGround;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public TileMapRender(GameObject.GameObject parentGOH, String fileName)
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

            Int32[] input = { 1, 7, 2, 2, 3, /**/ 15, 16, 14, 16, 17 };
            mMapData = new List<Int32>(input);

            TileMapRenderDefinition def = GameObjectManager.pInstance.pContentManager.Load<TileMapRenderDefinition>(fileName);

            mTexture = GameObjectManager.pInstance.pContentManager.Load<Texture2D>(def.mSpriteFileName);

            mSourceWidth = 16;
            mSourceHeight = 16;

            mMapCountWidth = mTexture.Width / mSourceWidth;
            mMapCountHeight = mTexture.Height / mSourceHeight;

            var shape = new PolygonShape();
            shape.SetAsBox(PhysicsManager.pInstance.ScreenToPhysicalWorld(mSourceWidth / 2 * 5),
                           PhysicsManager.pInstance.ScreenToPhysicalWorld(mSourceHeight / 2 * 2));

            var fd = new FixtureDef();
            fd.shape = shape;
            fd.restitution = 0.0f;
            fd.friction = 0.5f;
            fd.density = 0.0f;

            BodyDef bd = new BodyDef();
            mGround = PhysicsManager.pInstance.pWorld.CreateBody(bd);
            mGround.CreateFixture(fd);

            shape.SetAsBox(PhysicsManager.pInstance.ScreenToPhysicalWorld(mSourceWidth / 2 * 5),
                           PhysicsManager.pInstance.ScreenToPhysicalWorld(mSourceHeight / 2 * 2),
                           PhysicsManager.pInstance.ScreenToPhysicalWorld(new Vector2(mSourceWidth * -5, 0.0f)),
                           0.0f);
            fd = new FixtureDef();
            fd.shape = shape;
            fd.restitution = 0.0f;
            fd.friction = 0.5f;
            fd.density = 0.0f;
            mGround.CreateFixture(fd);

        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            mGround.SetTransform(PhysicsManager.pInstance.ScreenToPhysicalWorld(
                mParentGOH.pOrientation.mPosition.X + (mSourceWidth / 2 * 5) - (mSourceWidth / 2),
                mParentGOH.pOrientation.mPosition.Y + (mSourceHeight / 2 * 2) - (mSourceHeight / 2)), 
                0.0f);
        }

        /// <summary>
        /// Called once render cycle by the game object manager.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        public override void Render(SpriteBatch batch)
        {
            Vector2 pos = mParentGOH.pOrientation.mPosition;

            for (Int32 i = 0; i < mMapData.Count; i++)
            {
                if (i == mMapData.Count / 2)
                {
                    pos.X = mParentGOH.pOrientation.mPosition.X;
                    pos.Y += mSourceHeight;
                }

                // Convert the linear map data into a 2D array which matches out source texture.
                Int32 x = mMapData[i] % mMapCountWidth;
                Int32 y = mMapData[i] / mMapCountWidth;

                batch.Draw(mTexture,
                           pos,
                           new Microsoft.Xna.Framework.Rectangle(x * mSourceWidth, y * mSourceHeight, mSourceWidth, mSourceHeight),
                           Color.White,
                           mParentGOH.pOrientation.mRotation,
                           new Vector2(mSourceWidth * 0.5f, mSourceHeight * 0.5f),
                           mParentGOH.pOrientation.mScale,
                           SpriteEffects.None,
                           0);

                pos.X += mSourceWidth;
            }
        }
    }
}
