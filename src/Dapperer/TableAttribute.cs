﻿using System;

namespace Dapperer
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Class, AllowMultiple = false)]
    public class TableAttribute : Attribute
    {
        public string Name { get; private set; }

        public TableAttribute(string name)
        {
            Name = name;
        }
    }
}