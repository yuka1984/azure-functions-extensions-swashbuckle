﻿using System.Reflection;

namespace AzureFunctions.Extensions.Swashbuckle
{
    internal class SwashBuckleStartupConfig
    {
        public Assembly Assembly { get; set; }

        public string AppDirectory { get; set; }
    }
}