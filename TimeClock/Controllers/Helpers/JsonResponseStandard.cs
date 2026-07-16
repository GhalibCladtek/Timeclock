using System;
using System.Text;
using System.Web.Security; // Required for MachineKey

namespace TimeClock.Helpers
{
    public static class JsonResponseStandart
    {
        // A unique string to ensure this specific encryption can't be mixed up with others
        public const string success = "success";
        public const string failed = "failed";
        public const string error = "error";
    }
}