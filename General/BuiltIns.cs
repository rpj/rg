using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Roentgenium.Attributes;
using Roentgenium.Interfaces;
using Roentgenium.Stages.Sinks;
using Roentgenium.Stages.Persistence;
using Roentgenium.Stages.Intermediate;

namespace Roentgenium
{
    public static class BuiltIns
    {
        public static BindingFlags SpecPropertyFlags = BindingFlags.Instance | BindingFlags.Public;

        public static readonly string Version;

        public static readonly string Name;

        public static readonly List<Type> SpecTypes;

        public static readonly Dictionary<string, Type> Filters;

        public static readonly List<string> SupportedSpecs;

        public static readonly List<string> SupportedFilters;

        public static readonly List<Type> SinkStages;

        public static readonly List<Type> PersistenceStages;

        public static readonly Dictionary<string, Type> OutputSinks;

        static BuiltIns()
        {
            var ea = Assembly.GetExecutingAssembly();
            var asmbTypes = ea.GetTypes();

            Version = ea.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
            Name = ea.GetCustomAttribute<AssemblyTitleAttribute>().Title;

            // list all ISpecifications with at least one custom attribute
            // (i.e. the required SpecificationAttribute)
            SpecTypes = asmbTypes.Where(t => t.GetInterfaces().Contains(typeof(ISpecification))
                            && t.GetCustomAttributes(false)
                                .Any(ca => ca.GetType() == typeof(SpecificationAttribute))).ToList();

            SupportedSpecs = SpecTypes.SelectMany(t => t.GetCustomAttributes(false))
                    .Select(sa => ((SpecificationAttribute)sa).SpecType.ToString()).ToList();

            // Intermediate stages are "filters"
            Filters = asmbTypes.Where(t => t.GetInterfaces().Contains(typeof(IIntermediateStage)) && t != typeof(IntermediateStageBase))
                    .ToDictionary(ks => ks.Name.Replace("Intermediate", "").Replace("Stage", ""));

            SupportedFilters = Filters.Keys.ToList();

            SinkStages = asmbTypes.Where(t => t.GetInterfaces().Contains(typeof(ISinkStage)))
                .Where(t => t.BaseType == typeof(SinkStageBase)).ToList();

            PersistenceStages = asmbTypes.Where(t => t.GetInterfaces().Contains(typeof(IPersistenceStage)))
                .Where(t => t.BaseType == typeof(PersistenceStageBase)).ToList();

            OutputSinks = SinkStages.Where(t => t.GetCustomAttributes(false).Any(ca => ca is OutputFormatSinkType))
                .ToDictionary(ks => ks.Name.Replace("Sink", "").ToLower(), vs => vs);
        }
    }
}
