using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ModConsole
{
    internal static class Inspector
    {
        internal enum InspectionType
        {
            Fields,
            Properties
        }

        public static string Inspect(object result, InspectionType type = InspectionType.Fields)
        {
            switch (result)
            {
                case null:
                    return "null";

                case string str:
                    return str;
            }

            Type rType = result.GetType();

            var sb = new StringBuilder();

            sb.AppendLine($"[{rType}]");
            sb.AppendLine($"{result}");

            FieldInfo[] fields = rType.GetFields(BindingFlags.Public | BindingFlags.Instance).OrderBy(x => x.Name).ToArray();
            PropertyInfo[] properties = rType.GetProperties(BindingFlags.Public | BindingFlags.Instance).OrderBy(x => x.Name).ToArray();

            if (fields.Length == 0)
                type = InspectionType.Properties;

            if (properties.Length == 0)
                type = InspectionType.Fields;

            switch (type)
            {
                case InspectionType.Fields:
                    foreach (FieldInfo field in fields) AppendMemberInfo(result, field, sb);
                    break;
                case InspectionType.Properties:
                    foreach (PropertyInfo property in properties) AppendMemberInfo(result, property, sb);
                    break;
            }

            return sb.ToString();
        }

        private static void AppendMemberInfo(object result, MemberInfo member, StringBuilder sb)
        {
            object value = null;

            try
            {
                switch (member)
                {
                    case PropertyInfo p:
                    {
                        // Indexer propertty
                        if (p.GetIndexParameters().Length != 0)
                            return;

                        value = p.GetValue(result, null);
                        break;
                    }

                    case FieldInfo fi:
                    {
                        value = fi.GetValue(result);
                        break;
                    }
                }
            }
            catch (TargetInvocationException)
            {
                // yeet 
            }

            sb.Append("<color=#14f535>").Append(member.Name.PadRight(30)).Append("</color>");

            switch (value)
            {
                case string s:
                    sb.AppendLine(s);
                    break;
                case IEnumerable e:
                    IEnumerable<object> collection = e.Cast<object>();

                    // Don't have multiple enumerations
                    IEnumerable<object> enumerated = collection as object[] ?? collection.ToArray();

                    int count = enumerated.Count();

                    Type type = enumerated.FirstOrDefault()?.GetType();

                    if ((type?.IsPrimitive ?? false) || type == typeof(string))
                    {
                        sb.Append("[");

                        sb.Append(string.Join(", ", enumerated.Take(Math.Min(5, count)).Select(x => x.ToString()).ToArray()));

                        if (count > 5)
                            sb.Append(", ...");

                        sb.AppendLine("]");
                    }
                    else
                    {
                        sb.AppendLine($"Item Count: {count}");
                    }

                    break;
                default:
                    sb.AppendLine(value?.ToString() ?? "null");
                    break;
            }
        }
    }
}