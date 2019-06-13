using Roentgenium.Attributes;
using Roentgenium.FieldGenerators;
using Roentgenium.Interfaces;

namespace Roentgenium.Specifications
{
    [Specification(SpecificationType.Companies)]
    internal class CompaniesSpecification : ISpecification
    {
        [GeneratorType(typeof(BusinessNameFieldGenerator))]
        public string Company { get; set; }
    }
}