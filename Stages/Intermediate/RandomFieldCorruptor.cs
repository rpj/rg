using System.Linq;
using System.Reflection;
using Roentgenium.FieldGenerators;

namespace Roentgenium.Stages.Intermediate
{
    public class RandomFieldCorruptorStage : IntermediateStageBase
    {
        protected override PropertyInfo ConcretePropertySelector()
        {
            var props = _current.GetType().GetProperties(BuiltIns.SpecPropertyFlags).ToList();
            return props[GeneratorsStatic.Random.Next(props.Count)];
        }
    }
}
