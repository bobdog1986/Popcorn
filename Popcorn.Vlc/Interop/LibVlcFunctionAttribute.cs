using System;

namespace Popcorn.Vlc.Interop
{
    [AttributeUsage(AttributeTargets.Delegate, AllowMultiple = true)]
    public class LibVlcFunctionAttribute : Attribute
    {
        public LibVlcFunctionAttribute(string functionName)
            : this(functionName, null)
        {
        }
        
        public LibVlcFunctionAttribute(string functionName, string minVersion)
            : this(functionName, minVersion, null)
        {
        }
        
        public LibVlcFunctionAttribute(string functionName, string minVersion, string maxVersion)
            : this(functionName, minVersion, maxVersion, null)
        {
        }
        
        public LibVlcFunctionAttribute(string functionName, string minVersion, string maxVersion, string dev)
        {
            FunctionName = functionName;
            if (minVersion != null)
                MinVersion = new Version(minVersion);
            if (maxVersion != null)
                MaxVersion = new Version(maxVersion);
            if (dev != null)
                Dev = dev;
        }
        
        public string FunctionName { get; private set; }
        
        public Version MinVersion { get; private set; }
        
        public Version MaxVersion { get; private set; }
        
        public String Dev { get; private set; }
    }
}