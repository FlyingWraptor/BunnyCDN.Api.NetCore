using System;
using System.Text.RegularExpressions;

namespace BunnyCDN.Api.Internals
{
    internal static class Regexes
    {
        internal static readonly Regex AccountToken = new Regex(@"^([0-9a-fA-F]{8})(-[0-9a-fA-F]{4}){3}-([0-9a-fA-F]{20})(-[0-9a-fA-F]{4}){3}-([0-9a-fA-F]{12})$");
        internal static readonly Regex StorageToken = new Regex(@"^([0-9a-fA-F]{8})(-[0-9a-fA-F]{4}){2}-([0-9a-fA-F]{12})(-[0-9a-fA-F]{4}){2}$");
        internal static readonly Regex StorageName = new Regex(@"^([-a-zA-Z0-9]){3,20}$");
        internal static readonly Regex PullZoneName = new Regex(@"^([a-zA-Z0-9]){3,20}$");
        internal static readonly Regex Base64String = new Regex(@"^[a-zA-Z0-9\+/]*={0,2}$");
    }
}