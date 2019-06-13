using System.Linq;
using System.Reflection;
using Roentgenium.Attributes;
using Roentgenium.Interfaces;

namespace Roentgenium.Stages.Intermediate
{
    public class SequenceNumberCorruptorStage : IntermediateStageBase, IIntermediateStage
    {
        protected override PropertyInfo ConcretePropertySelector()
        {
            return _current.GetType().GetProperties(BuiltIns.SpecPropertyFlags)
                .ToList().FirstOrDefault(prop => prop.GetCustomAttributes(false)
                .Any(attr => attr.GetType() == typeof(SequenceNumberAttribute)));
        }
    }
}
