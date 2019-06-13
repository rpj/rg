using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Roentgenium.Attributes;
using Roentgenium.Interfaces;
using Roentgenium.FieldGenerators;

namespace Roentgenium.Stages.Intermediate
{
    public abstract class IntermediateStageBase : IIntermediateStage
    {
        protected IGeneratedRecord _current;
        protected double Frequency = 0.5;    // TODO: CONFIG'ED! (have to figure out pass-down...)

        public IntermediateStageBase()
        {
            Console.WriteLine($"${GetType().Name} filter enabled with frequency {Frequency}");
        }

        protected abstract PropertyInfo ConcretePropertySelector();

        public bool Prepare() { return true; }

        public bool Sink(IGeneratedRecord record)
        {
            _current = record;
            return true;
        }

        public IGeneratedRecord Next(Type type, uint seqNo)
        {
            if (new Random().Next((int)(1.0 / Frequency)) == 0)
            {
                var target = ConcretePropertySelector();
                var curVal = target.GetValue(_current);

                var fieldGen = new List<Type>() {
                    ((GeneratorTypeAttribute)target.GetCustomAttributes(true)
                        .FirstOrDefault(ca => ca.GetType() == typeof(GeneratorTypeAttribute)))?.Type,
                    GeneratorsStatic.DefaultFieldGeneratorTypes[target.PropertyType],
                    target.PropertyType
                }
                .Where(lt => lt != null && GeneratorsStatic.GeneratorCache.ContainsKey(lt))
                .Select(lt => GeneratorsStatic.GeneratorCache[lt])
                .FirstOrDefault();

                if (fieldGen == null)
                    throw new InvalidProgramException($"No generator available for {target}");

                var fakeConfig = new FieldGeneratorOptions();
                var newVal = fieldGen.GenerateField(ref fakeConfig);
#if DEBUG
                Console.WriteLine($"{GetType().Name} altering #{seqNo}'s field '{target.Name}' from '{curVal}' to '{newVal}'!");
#endif
                target.SetValue(_current, newVal);
            }

            return _current;
        }

        public SinkStageArtifact Finish() { return new SinkStageArtifact(this); }
    }
}