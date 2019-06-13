using Roentgenium.Attributes;
using Roentgenium.Interfaces;
using System;
using System.Linq;
using Roentgenium.FieldGenerators;
using Roentgenium.Config;

namespace Roentgenium.Stages.Sources
{
    public class GeneratorSource : ISourceStage
    {
        private GeneratorConfig _config;

        public GeneratorSource(ref GeneratorConfig config)
        {
            _config = config;
        }

        public IGeneratedRecord Next(Type spec, uint seqNo)
        {
            var aNewRec = Activator.CreateInstance(spec);
            spec.GetProperties(BuiltIns.SpecPropertyFlags).ToList().ForEach(p =>
            {
                // build the options and handle any special attrs for this field
                var gOpts = GeneratorsStatic.ParseGeneratorOptionPairs(p);
                gOpts.Config = _config;

                var fieldGen = GeneratorsStatic.DefaultFieldGeneratorTypes[p.PropertyType];
                var isSeqNo = false;
                FieldLinkerTypeAttribute linker = null;

                p.GetCustomAttributes(false).ToList().ForEach(cAttr =>
                {
                    if (cAttr is GeneratorTypeAttribute)
                    {
                        fieldGen = ((GeneratorTypeAttribute)cAttr).Type;
                    }
                    else if (cAttr is SequenceNumberAttribute)
                    {
                        isSeqNo = true;
                    }
                    else if (cAttr is FieldLinkerTypeAttribute)
                    {
                        linker = (FieldLinkerTypeAttribute)cAttr;
                    }
                });
                
                // verify the field generator options, specifically that:
                // - SequenceNumberAttribute can only be applied to 'uint' and 'ulong' fields
                // - SequenceNumberAttribute and FieldLinkerTypeAttribute cannot coexist on a field
                if (isSeqNo && p.PropertyType != typeof(uint) && p.PropertyType != typeof(ulong))
                    throw new InvalidOperationException($"SequenceNumberAttribute applied to field {p}, " +
                        $"which is of type {p.PropertyType}: it must be 'uint' or 'ulong'");
                
                if (isSeqNo && linker != null)
                    throw new InvalidProgramException("Sequence number field cannot be also be linked");
                
                // generate the new field by (in order):
                // - linking it to another field if specified, or
                // - using the current sequence number if specified, or
                // - make blank if specified and the random choice comes up positive, or
                // - generate using the specified field generator and options.
                object generated;
                if (linker != null)
                {
                    var linkedField = spec.GetProperty(linker.LinkedFieldName, BuiltIns.SpecPropertyFlags);
                    var linkerInst = (IFieldLinker)Activator.CreateInstance(linker.LinkerType);
                    generated = linkerInst.LinkField(linkedField.GetValue(aNewRec), ref gOpts);
                }
                else if (isSeqNo)
                {
                    generated = seqNo;
                }
                else if (gOpts.BlankFrequency != FieldGeneratorOptions.BlankFrequencyDisabled &&
                        new Random().Next(100) <= gOpts.BlankFrequency)
                {
                    generated = null;
                }
                else
                {
                    try 
                    {
                        generated = GeneratorsStatic.GeneratorCache[fieldGen].GenerateField(ref gOpts);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"GenerateField for '{p.Name}' failed at seqNo {seqNo} and spec {spec}: {ex.Message}");
                        throw;
                    }
                }

                p.SetValue(aNewRec, generated);
            });

            return (IGeneratedRecord)aNewRec;
        }
    }
}
