using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngineContentDefs;
using MBHEngine.GameObject;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MBHEngine.Behaviour
{
    /// <summary>
    /// Base class from which all behaviours should derive from.
    /// A behaviour is a collection of logic which can be attached to a game object.
    /// </summary>
    public abstract class Behaviour
    {
        /// <summary>
        /// The game object that this behaviour is attached to.
        /// </summary>
        protected GameObject.GameObject mParentGOH;

        /// <summary>
        /// If populated, the object will only be updated during these passes.
        /// </summary>
        protected List<BehaviourDefinition.Passes> mUpdatePasses;

        /// <summary>
        /// Do not render when the current GameObject pass is in this list.
        /// </summary>
        protected List<BehaviourDefinition.Passes> mRenderPassExclusions;

        /// <summary>
        /// If false, this behaviour will enter a state where it no longer receives 
        /// Updates, Render calls, or Messages.
        /// </summary>
        protected Boolean mIsEnabled;

        /// <summary>
        /// Constructor which also handles the process of loading in the Behaviour
        /// Definition information.
        /// </summary>
        /// <param name="parentGOH">The game object that this behaviour is attached to.</param>
        /// <param name="fileName">The file defining this behaviour.</param>
        public Behaviour(GameObject.GameObject parentGOH, String fileName)
        {
            mParentGOH = parentGOH;

            // This will call to the derived class's version of LoadContent which should trickle
            // down through each level.
            LoadContent(fileName);
        }

        /// <summary>
        /// Call this to initialize a Behaviour with data supplied in a file.
        /// </summary>
        /// <param name="fileName">The file to load from.</param>
        public virtual void LoadContent(String fileName)
        {
            // Default values for the cases where there is no template file.
            //
            mIsEnabled = true;

            if (null != fileName)
            {
                // No data stored here right now, so leaving it commented out for the time being.
                BehaviourDefinition def = GameObjectManager.pInstance.pContentManager.Load<BehaviourDefinition>(fileName);

                mUpdatePasses = def.mUpdatePasses;

                mRenderPassExclusions = def.mRenderPassExclusions;

                mIsEnabled = def.mIsEnabled;
            }

            if (null == mUpdatePasses)
            {
                mUpdatePasses = new List<BehaviourDefinition.Passes>(1);
                mUpdatePasses.Add(BehaviourDefinition.Passes.DEFAULT);
            }
        }

        /// <summary>
        /// Called at the end of the frame where mParentGOH was added to the GameObjectManager.
        /// </summary>
        public virtual void OnAdd()
        {
        }

        /// <summary>
        /// Called at the end of the frame on which this Behaviour's mParentGOH was removed from
        /// the GameObjectManager.
        /// </summary>
        public virtual void OnRemove()
        {
        }

        /// <summary>
        /// Called when the Behaviour goes from being disabled to enabled.
        /// This will NOT be called if the behaviour initialially starts enabled.
        /// </summary>
        public virtual void OnEnable()
        {
        }

        /// <summary>
        /// Called when the Behaviour goes from being enabled to disable.
        /// This will NOT be called if the behaviour initially starts disabled.
        /// </summary>
        public virtual void OnDisable()
        {
        }

        /// <summary>
        /// Called once per frame before the update function. Is called for ALL gameobjects, prior 
        /// to calling Update.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public virtual void PreUpdate(GameTime gameTime)
        {
        }

        /// <summary>
        /// Called once per frame by the game object.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public virtual void Update(GameTime gameTime)
        {
        }

        /// <summary>
        /// Called once per frame after the Update function. Is called after all objects have
        /// caled Update.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed this frame.</param>
        public virtual void PostUpdate(GameTime gameTime)
        {
        }

        /// <summary>
        /// Called once render cycle by the game object manager.
        /// </summary>
        /// <param name="batch">The sprite batch to render to.</param>
        /// <param name="effect">The currently set shader.</param>
        public virtual void Render(SpriteBatch batch, Effect effect)
        {
        }

        /// <summary>
        /// The main interface for communicating between behaviours.  Using polymorphism, we
        /// define a bunch of different messages deriving from BehaviourMessage.  Each behaviour
        /// can then check for particular upcasted messahe types, and either grab some data 
        /// from it (set message) or store some data in it (get message).
        /// </summary>
        /// <param name="msg">The message being communicated to the behaviour.</param>
        public virtual void OnMessage(ref BehaviourMessage msg) 
        {
        }

#if ALLOW_GARBAGE
        /// <summary>
        /// Returns a bunch of information about the behaviour which can be dumped to
        /// a debug display for debugging at runtime.
        /// </summary>
        /// <returns>A formatted string of debug information.</returns>
        public virtual String [] GetDebugInfo()
        {
            return null;
        }
#endif // ALLOW_GARBAGE

        /// <summary>
        /// Resets a behaviour to its initial state.
        /// </summary>
        public virtual void Reset()
        {
        }

        /// <summary>
        /// A list of all the passes that can be active for this object to recieve updates.
        /// </summary>
        public virtual List<BehaviourDefinition.Passes> pUpdatePasses
        {
            get
            {
                return mUpdatePasses;
            }
        }

        /// <summary>
        /// A list of all the passes that if currently active should signal this Behaviour to NOT be rendered.
        /// The Behaviour does not want to be rendered during these Passes. If null just always render.
        /// </summary>
        public virtual List<BehaviourDefinition.Passes> pRenderPassExclusions
        {
            get
            {
                return mRenderPassExclusions;
            }
        }

        /// <summary>
        /// Is this behaviour enabled right now.
        /// </summary>
        public Boolean pIsEnabled
        {
            get
            {
                return mIsEnabled;
            }
            set
            {
                mIsEnabled = value;
            }
        }
    }
}
