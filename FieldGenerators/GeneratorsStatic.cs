using Roentgenium.Attributes;
using Roentgenium.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Roentgenium.FieldGenerators
{
    using FieldGeneratorOptionsDict = Dictionary<FieldGeneratorOptionType, object>;

    public static class GeneratorsStatic
    {
        [ThreadStatic] public static Random Random;
        public readonly static Dictionary<Type, Type> DefaultFieldGeneratorTypes;
        public readonly static Dictionary<Type, IFieldGenerator> GeneratorCache;

        static GeneratorsStatic()
        {
            var _afg = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(IFieldGenerator))
                            && !t.Attributes.HasFlag(TypeAttributes.Abstract)).ToList();
            
            // build generator cache
            GeneratorCache = _afg.ToDictionary(ks => ks, ts => (IFieldGenerator)Activator.CreateInstance(ts));

            // get list of default field generators
            Dictionary<List<Type>, Type> _dfg = _afg.Where(t => t.GetCustomAttributes(false)
                    .Any(ca => ca.GetType() == typeof(DefaultGeneratorForTypeAttribute)))
                .ToDictionary(ks => ks.GetCustomAttributes(false).ToList()
                    .Select(ksCa => ((DefaultGeneratorForTypeAttribute)ksCa).Type).ToList(),
                    genType => genType);

            // this transform allows multiple default attributes on a single class 
            DefaultFieldGeneratorTypes = new Dictionary<Type, Type>();
            _dfg.Keys.ToList().ForEach(kTypeList =>
            {
                var genRef = _dfg[kTypeList];
                kTypeList.ForEach(kType => DefaultFieldGeneratorTypes[kType] = genRef);
            });
        }

        public static void ThreadInit()
        {
            // ThreadStatic-attributed variables should not be default-initialized
            // because such initialization will only happen once, not once-per-thread:
            // https://docs.microsoft.com/en-us/dotnet/api/system.threadstaticattribute?view=netcore-2.2#remarks
            Random = new Random();
        }

        public static FieldGeneratorOptions ParseGeneratorOptionPairs(PropertyInfo prop)
        {
            IDictionary optsInterDict = new FieldGeneratorOptionsDict();
            prop.GetCustomAttributes(false).ToList().ForEach(cAttr =>
            {
                if (cAttr.GetType() == typeof(GeneratorOptionPairAttribute))
                {
                    var gop = (GeneratorOptionPairAttribute)cAttr;
                    optsInterDict[gop.Key] = gop.Value;
                }
            });

            var retVal = new FieldGeneratorOptions();

            for (int i = (int)FieldGeneratorOptionType.NoOpt_FirstValue + 1;
                     i < (int)FieldGeneratorOptionType.NoOpt_LastValue; i++)
            {
                if (optsInterDict.Contains((FieldGeneratorOptionType)i))
                {
                    var asType = (FieldGeneratorOptionType)i;
                    retVal.GetType().GetField(asType.ToString()).SetValue(retVal, optsInterDict[asType]);
                }
            }

            return retVal;
        }
    }
}
