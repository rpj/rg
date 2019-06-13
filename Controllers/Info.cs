using Roentgenium.Config;
using Roentgenium.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Roentgenium.Controllers
{
    /// <summary>Support for GetValue() on either PropertyInfo and FieldInfo instances.</summary>
    public static class MemberInfoExtension
    {
        /// <summary>PropertyInfo and FieldInfo are very much alike and both inherit from MemberInfo,
        /// yet they don't have a shared interface that defines the GetValue/SetValue stuff? Lame!</summary>
        public static object GetMemberValue(this MemberInfo mi, object o)
        {
            return mi is PropertyInfo ? ((PropertyInfo)mi).GetValue(o) : 
                (mi is FieldInfo ? ((FieldInfo)mi).GetValue(o) : null);
        }
    }

    /// <summary>Returns information about the system.</summary>
    [Route("[controller]")]
    [ApiController]
    public class InfoController : ControllerBase
    {
        private readonly IPipelineManager _pipelineManager;
        private readonly LimitsConfig _limits;
        private static Dictionary<string, object> _supported = new Dictionary<string, object>()
            {
                { "specifications", BuiltIns.SupportedSpecs},
                { "filters", BuiltIns.SupportedFilters},
                { "outputs", BuiltIns.OutputSinks.Keys.ToList() },
            };

        public InfoController(IPipelineManager pm,
            IOptions<LimitsConfig> limits)
        {
            _pipelineManager = pm;
            _limits = limits.Value;
        }

        private static object Flatten(object obj)
        {
            var oType = obj?.GetType();

            if (oType != null && !oType.IsPrimitive && oType.IsSerializable &&
                !(obj is string || obj is Guid || obj is TimeSpan || obj is Dictionary<string, object> || 
                    obj is IDictionary<string, object> || obj is IEnumerable))
            {
                var rv = new Dictionary<string, object>();
                var nl = new List<MemberInfo>(oType.GetProperties());
                nl.AddRange(oType.GetFields());
                nl.ForEach(mi => 
                {
                    if (mi.GetCustomAttribute(typeof(NonSerializedAttribute)) == null)
                        rv[mi.Name.ToLower()] = mi.GetMemberValue(obj);
                });
                return (object)rv;
            }
            else if (obj != null && (obj is Dictionary<string, object> || obj is IDictionary<string, object>))
                return ((Dictionary<string, object>)obj).ToDictionary(ks => ks.Key.ToLower(), vs => Flatten(vs.Value));

            return obj;
        }

        private static object FindKeyPath(string[] ks, object d)
        {
            if (ks.Length > 0 && !string.IsNullOrEmpty(ks[0]) && d is Dictionary<string, object>)
            {
                var dd = (Dictionary<string, object>)d;
                if (dd.ContainsKey(ks[0]))
                    return dd[ks[0]] is Dictionary<string, object> ? 
                        FindKeyPath(new ArraySegment<string>(ks, 1, ks.Length - 1).ToArray(), dd[ks[0]]) : 
                        dd[ks[0]];
            }

            return d;
        }

        /// <summary>
        /// Get information about the system, optionally by key-path
        /// </summary>
        /// <param name="keyPath">
        /// An optional key-path to allow more-granular queries, such as
        /// <a href="/info/supported/specifications" target="_blank">
        /// <code>info/supported/specifications</code></a>, <a href="/info/gestalt/stats/uptime" target="_blank">
        /// <code>info/gestalt/stats/uptime</code>
        /// </a> or (up one level from the last) <a href="/info/gestalt/stats" target="_blank">
        /// <code>info/gestalt/stats</code></a>.
        /// </param>
        [HttpGet("{*keyPath}")]
        [ProducesResponseType(typeof(Dictionary<string, object>), 200)]
        [Produces("application/json")]
        public JsonResult RootGet(string keyPath = null)
        {
            var pieces = new Dictionary<string, object>()
            {
                { "defaults", new GeneratorConfig() },
                { "supported", _supported },
                { "gestalt",  new Dictionary<string, object>() {
                    { "version", BuiltIns.Version },
                    { "name", BuiltIns.Name },
                    { "stats", _pipelineManager.Lifetime() }
                } },
                { "jobs", _pipelineManager.Info() },
                { "limits", _limits }
            };

            var ps = keyPath?.Split('/');

            return new JsonResult(string.IsNullOrEmpty(keyPath) || ps?.Length < 1 ? 
                pieces : FindKeyPath(ps, Flatten(pieces)));
        }
    }
}