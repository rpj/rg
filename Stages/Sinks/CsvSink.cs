using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Roentgenium.Interfaces;
using CsvHelper;
using Roentgenium.Attributes;
using Roentgenium.Config;
using CsvHelper.TypeConversion;
using CsvHelper.Configuration;

namespace Roentgenium.Stages.Sinks
{
    [OutputFormatSinkType]
    public class CsvSink : SinkStageBase
    {
        protected CsvWriter _csv;
        protected List<PropertyInfo> _fields;

        public CsvSink(GeneratorConfig gCfg) : base(gCfg)
        {
            _fields = gCfg.TypedSpecification.GetProperties(BuiltIns.SpecPropertyFlags).ToList();
        }

        public override bool Sink(IGeneratedRecord inRec)
        {
            _fields.ForEach(field =>
            {
                var nextVal = field.GetValue(inRec);
                _csv.WriteField(nextVal == null ? "" : nextVal);
            });
            _csv.NextRecord();
            return true;
        }

        private class BooleansAsOnesAndZeros : BooleanConverter
        {
            public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
            {
                return value is bool ? 
                    ((bool)value ? "1" : "0") : 
                    base.ConvertToString(value, row, memberMapData);
            }
        }

        public override bool Prepare()
        {
            if (!base.Prepare())
                return false;

            _csv = new CsvWriter(Writer);
            _csv.Configuration.TypeConverterCache.AddConverter<bool>(new BooleansAsOnesAndZeros());

            // properly handle any FormatFieldOutputSpecAttributes
            var headerNames = new List<string>(_fields.Count);
            _fields.ForEach(fpi =>
            {
                var name = fpi.Name;
                fpi.GetCustomAttributes(false)
                    .Where(ca => ca.GetType() == typeof(FormatFieldOutputSpecAttribute))
                    .ToList().ForEach(ffosa =>
                    {
                        var ff = (FormatFieldOutputSpecAttribute) ffosa;
                        if (ff.SpecType == GetType())
                            name = ff.OutputName;
                    });
                headerNames.Add(name);
            });
            
            headerNames.ForEach(h => _csv.WriteField(h));
            _csv.NextRecord();
            return true;
        }
    }
}
