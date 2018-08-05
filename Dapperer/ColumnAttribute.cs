using System;

namespace Dapperer
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
    public class ColumnAttribute : Attribute
    {
        public string Name { get; }
        public bool IsPrimary { get; set; }
        public bool AutoIncrement { get; set; }

        public ColumnAttribute(string name)
        {
            Name = name;
        }
    }
}