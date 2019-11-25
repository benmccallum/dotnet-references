using System.Collections.Generic;

namespace BenMcCallum.DotNet.References
{
    public enum ErrorCode
    {
        Unknown = 1,
        ModeArgInvalid = 10,
        EntryPointArgInvalid = 20,
        WorkingDirectoryArgInvalid = 30,

    }

    public static class Errors
    {
        public static Dictionary<ErrorCode, string> ErrorMessages = new Dictionary<ErrorCode, string>()
        {
            { ErrorCode.Unknown, "An unknown error occurred." },
            { ErrorCode.ModeArgInvalid, "An invalid Mode argument was given." },
            { ErrorCode.EntryPointArgInvalid, "An invalid Entry Point argument was given." },
            { ErrorCode.WorkingDirectoryArgInvalid, "An invalid Working Directory argument was given." }
        };
    }
}
