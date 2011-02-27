using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MBHEngine.Behaviour
{
    /// <summary>
    /// The base class from which all behaviour messages should inherrit from.
    /// The main interface for communicating between behaviours.  Using polymorphism, we
    /// define a bunch of different messages deriving from BehaviourMessage.  Each behaviour
    /// can then check for particular upcasted messahe types, and either grab some data 
    /// from it (set message) or store some data in it (get message).
    /// </summary>
    public interface BehaviourMessage
    {
    }
}
