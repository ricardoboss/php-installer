using System;

namespace PHP_Installer.Exceptions
{
    public sealed class UpdateException : Exception
    {
        public UpdateException(string s) : base(s) {}
    }
}
