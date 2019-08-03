using System;

namespace Efficient.Json.Tokens
{
    [Flags]
    internal enum TokenType
    {
        None,
        Error = 0x0001,

        // Values
        True = 0x0002,
        False = 0x0004,
        Null = 0x0008,
        String = 0x0010,
        EncodedString = 0x0020,
        Number = 0x0040,

        // Containers
        OpenBracket = 0x0100,
        CloseBracket = 0x0200,
        OpenCurly = 0x0400,
        CloseCurly = 0x0800,
        Colon = 0x1000,
        Comma = 0x2000,

        // Container values
        ArrayValue = 0x10000,
        ObjectValue = 0x20000,

        // Checks
        AnyBoolean = True | False,
        AnyString = String | EncodedString,
        AnyValue = True | False | Null | String | EncodedString | Number,
    }
}
