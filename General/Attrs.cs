using System;
using Roentgenium.Interfaces;

namespace Roentgenium.Attributes
{
    /// <summary>
    /// Defines which SpecificationType a given ISpecification-implementing class applies to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SpecificationAttribute : Attribute
    {
        public SpecificationType SpecType { get; }

        public SpecificationAttribute(SpecificationType specType)
        {
            SpecType = specType;
        }
    }

    /// <summary>
    /// Denotes that the IFieldGenerator-implementing class to which it is applied
    /// is the default generator for the type it generates. May be applied multiple
    /// times to allow implicitly-convertible types to share default generators.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    // TODO: what allows us to restrict which interfaces this can applied to? necessary?
    public class DefaultGeneratorForTypeAttribute : Attribute
    {
        public Type Type { get; }

        public DefaultGeneratorForTypeAttribute(Type type)
        {
            Type = type;
        }
    }

    /// <summary>
    /// Marks a subclass of SinkStageBase as an output format. It must be named
    /// "TypeSink", where "Type" denotes the output format name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class OutputFormatSinkType : Attribute { }

    /// <summary>
    /// Specifies an alternative field name (for a given format)
    /// to be used during the sink stage.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class FormatFieldOutputSpecAttribute : Attribute
    {
        public Type SpecType { get; }
        public string OutputName { get; }

        public FormatFieldOutputSpecAttribute(Type sinkStageType, string name)
        {
            SpecType = sinkStageType;
            OutputName = name;
        }
    }

    /// <summary>
    /// Specifies which IFieldGenerator-implementing type should be
    /// used to generate this field in the source stage.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class GeneratorTypeAttribute : Attribute
    {
        public Type Type;

        public GeneratorTypeAttribute(Type genType)
        {
            Type = genType;
        }
    }

    /// <summary>
    /// Specifies a key/value pair option to be passed to the
    /// generator defined for this type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class GeneratorOptionPairAttribute : Attribute
    {
        public FieldGeneratorOptionType Key { get; }
        public object Value { get; }

        public GeneratorOptionPairAttribute(FieldGeneratorOptionType key, object value)
        {
            Key = key;
            Value = value;
        }
    }

    /// <summary>
    /// Specifies which IFieldLinker-implementing type should be
    /// used to link this field to another in the specification.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class FieldLinkerTypeAttribute : Attribute
    {
        public string LinkedFieldName;
        public Type LinkerType;

        public FieldLinkerTypeAttribute(string linkedFieldName, Type linkerType)
        {
            LinkedFieldName = linkedFieldName;
            LinkerType = linkerType;
        }
    }

    /// <summary>
    /// Specifies that the given field (must be an int or long) be
    /// assigned the monotonicially-increasing sequence number.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SequenceNumberAttribute : Attribute { }
}
