using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MathFreak.Math
{
    /// <summary>
    /// Base class for all types of variable used in the internal math representation.
    /// 
    /// NOTE: all math variable types are coded so that they are immutable - the values contained in
    /// any math variable cannot be changed once the variable has been created; operations on math
    /// variables will return new instances instead of modifying the variables themselves (this
    /// garrauntees that variables can be relied upon not to change their values unexpectedly).
    /// </summary>
    public abstract class MathVariable : MathElement
    {
    }
}
