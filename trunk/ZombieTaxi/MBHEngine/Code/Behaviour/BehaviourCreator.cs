using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBHEngine.GameObject;

namespace MBHEngine.Behaviour
{
    /// <summary>
    /// The game object class is responsible for creating and attaching behaviours to itself.  To do so,
    /// it simply goes through a switch off all the behaviours it knows about, and creates them.  However,
    /// because some behaviours are defined by the client, the engine will not know about it.  By implementing
    /// this interface, and registering with the GameObjectManager, clients can extend the functionality to include
    /// their own behaviours.
    /// </summary>
    public interface BehaviourCreator
    {
        /// <summary>
        /// Helper function for creating behaviours based on strings of matching names.
        /// </summary>
        /// <param name="go">The game object that this behaviour is being attached to.</param>
        /// <param name="behaviourType">The name of the behaviour class we are creating.</param>
        /// <param name="fileName">The name of the file containing the behaviour definition.</param>
        /// <returns>The newly created behaviour.</returns>
        Behaviour CreateBehaviourByName(MBHEngine.GameObject.GameObject go, String behaviourType, String fileName);
    }
}
