using System;
using System.Collections.Generic;
using System.Text;

namespace OptimizelySDK.XUnitTests.UtilsTests
{
    public class UtilsData
    {
        public static IEnumerable<object[]> ValidRevenueTagsData =>
        new List<object[]>
        {
            new object[] { new Dictionary<string, object>() {{ "revenue", 42 }}, 42 },
            new object[] { new Dictionary<string, object>() {{ "revenue", 100 }}, 100 },
            new object[] { new Dictionary<string, object>() {{ "revenue", "123" }}, 123 },
        };

        public static IEnumerable<object[]> InvalidRevenueTagsData =>
        new List<object[]>
        {
            new object[] { null },
            new object[] { new Dictionary<string, object>() {{ "abc", 42 }} },
            new object[] { new Dictionary<string, object>() {{ "revenue", 42.5 }} },
            new object[] { new Dictionary<string, object>() {{ "non-revenue", 42 }} }
        };
        
        public static IEnumerable<object[]> InvalidRevenueNotInt =>
        new List<object[]>
        {
            new object[] { new Dictionary<string, object>() {{ "revenue", 42.5 }} },
        };
    
        public static IEnumerable<object[]> NumericMetricInvalidValueTagData =>
        new List<object[]>
        {
            new object[] { new Dictionary<string, object>() },
            new object[] { new Dictionary<string, object>() {{ "non-value", null } } },
            new object[] { new Dictionary<string, object>() {{ "non-value", 0.5 } } },
            new object[] { new Dictionary<string, object>() {{ "non-value", 12345 } } },
            new object[] { new Dictionary<string, object>() {{ "non-value", "65536" } } },
            new object[] { new Dictionary<string, object>() {{ "non-value", true } } },
            new object[] { new Dictionary<string, object>() {{ "non-value", false } } },
            new object[] { new Dictionary<string, object>() {{ "non-value", new object[] { 1, 2, 3 } } } },
            new object[] { new Dictionary<string, object>() {{ "non-value", new object[] { 'a', 'b', 'c' } } } },
        };
        
        public static IEnumerable<object[]> NumericMetricValueValidTagData =>
        new List<object[]>
        {
            new object[] { new Dictionary<string, object>() {{ "value", 12345 }}, 12345.0f },
            new object[] { new Dictionary<string, object>() {{ "value", "12345" }}, 12345.0f },
            new object[] { new Dictionary<string, object>() {{ "value", 1.2345F }}, 1.2345f },
            new object[] { new Dictionary<string, object>() {{ "value", float.MaxValue } }, float.MaxValue },
            new object[] { new Dictionary<string, object>() {{ "value", float.MinValue } }, float.MinValue }
        };

        public static IEnumerable<object[]> InvalidTagsKeyNotDefined =>
        new List<object[]>
        {
            new object[] { new Dictionary<string, object>() {{ "abc", 42 }} },
            new object[] { new Dictionary<string, object>() {{ "non-revenue", 42 }} }
        };

        public static IEnumerable<object[]> InvalidTagsUnDefined =>
        new List<object[]>
        {
           new object[] { null }
        };
        
        public static IEnumerable<object[]> InvalidTagsNotDefinedInEventTags =>
        new List<object[]>
        {
           new object[] { new Dictionary<string, object>() {{ "revenue", null }} },
        };
        public static IEnumerable<object[]> ValidEventValueData =>
        new List<object[]>
        {
            new object[] { new Dictionary<string, object>() {{ "value", 42 }}, 42f },
            new object[] { new Dictionary<string, object>() {{ "value", 42.5 } }, 42.5f },
            new object[] { new Dictionary<string, object>() {{ "value", 42.52 } }, 42.52f },
            new object[] { new Dictionary<string, object>() {{ "value", "42" } }, 42f },
            new object[] { new Dictionary<string, object>() {{ "value", "42.3" } }, 42.3f },
        };
    }
}
