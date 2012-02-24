using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MBHEngine.GameObject;
using MBHEngine.Behaviour;

namespace ZombieTaxi.Behaviours
{
    class Kamikaze : MBHEngine.Behaviour.Behaviour
    {
        /// <summary>
        /// Preallocated messages to avoid GC.
        /// </summary>
        private PathFind.SetDestinationMessage mSetDestinationMsg;
        private PathFind.SetSourceMessage mSetSourceMsg;
        private PathFind.GetCurrentBestNode mGetCurrentBestNodeMsg;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public Kamikaze(GameObject parentGOH, String fileName)
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

            // The game object manager will automatically move the game object by this speed,
            // in its forward direcion.
            mParentGOH.pDirection.mSpeed = 0.3f;

            // Allocate these ahead of time to avoid triggering GC.
            mSetDestinationMsg = new PathFind.SetDestinationMessage();
            mSetSourceMsg = new PathFind.SetSourceMessage();
            mGetCurrentBestNodeMsg = new PathFind.GetCurrentBestNode();
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public override void Update(GameTime gameTime)
        {
            GameObject player = GameObjectManager.pInstance.pPlayer;

            // Don't try anything until the player has actually be loaded.
            if (player == null) return;

            // Get the curent path to the player. It may not be complete at this point, but should include enough
            // information to start moving.
            mParentGOH.OnMessage(mGetCurrentBestNodeMsg);

            // If we have a best node chosen (again maybe not a complete path, but the best so far), start
            // moving towards the next point on the path.
            if (mGetCurrentBestNodeMsg.mBest != null)
            {
                // This is the node closest to the destination that we have found.
                PathFind.PathNode p = mGetCurrentBestNodeMsg.mBest;

                // Traverse back towards the source node until the previous one has already been reached.
                // That means the current one is the next one that has not been reached yet.
                while (p.mPrev != null && !p.mPrev.mReached)
                {
                    p = p.mPrev;
                }

                // Once we are within one unit of the target consider it reached.
                // TODO: This should realy take into account the movement speed of the GOH.
                if (Vector2.Distance(p.mTile.mCollisionRect.pCenterPoint, mParentGOH.pOrientation.mPosition) <= 1.0f)
                {
                    // This node has been reached, so next update it will start moving towards the next node.
                    p.mReached = true;

                    // However, if this is the destination node (or the closest we have found so far), then
                    // recalculate a path starting here.
                    if (mGetCurrentBestNodeMsg.mBest == p)
                    {
                        mSetSourceMsg.mSource = mParentGOH.pOrientation.mPosition;
                        mParentGOH.OnMessage(mSetSourceMsg);
                        mSetDestinationMsg.mDestination = player.pOrientation.mPosition;
                        mParentGOH.OnMessage(mSetDestinationMsg);
                    }
                }

                // Move towards the nodes center point.
                Vector2 d = p.mTile.mCollisionRect.pCenterPoint - mParentGOH.pOrientation.mPosition;
                d = Vector2.Normalize(d);
                mParentGOH.pDirection.mForward = d;
            }
            else
            {
                // If we don't have a destination set yet, set it up now.
                mSetSourceMsg.mSource = mParentGOH.pOrientation.mPosition;
                mParentGOH.OnMessage(mSetSourceMsg);
                mSetDestinationMsg.mDestination = player.pOrientation.mPosition;
                mParentGOH.OnMessage(mSetDestinationMsg);
            }
        }
    }
}
