using System;

namespace SharpDaemon
{
    public static class ConfigTools
    {
        public static void SetProperty(object target, string line)
        {
            var parts = line.Split(new char[] { '=' });
            if (parts.Length != 2) throw ExceptionTools.Make("Expected 2 parts in {0}", TextTools.Readable(line));
            var propertyName = parts[0];
            var propertyValue = parts[1];
            var property = target.GetType().GetProperty(propertyName);
            if (property == null) throw ExceptionTools.Make("Property not found {0}", TextTools.Readable(propertyName));
            var value = Convert.ChangeType(propertyValue, property.PropertyType);
            property.SetValue(target, value, null);
        }
    }
}
