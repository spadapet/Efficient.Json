namespace Efficient.Json.Path
{
    internal enum QueryTokenType
    {
        None,
        Error,

        Identifier,
        String,
        EncodedString,
        Number,

        And, // &&
        At, // @
        Bang, // !
        BangEqual, // !=
        CloseBracket, // ]
        CloseParen, // )
        Colon, // :
        Comma, // ,
        Dollar, // $
        Dot, // .
        DotDot, // ..
        Equal, // ==
        EqualTilde, // =~
        Greater, // >
        GreaterEqual, // >=
        Less, // <
        LessEqual, // <=
        OpenBracket, // [
        OpenParen, // (
        Or, // ||
        Question, // ?
        Regex, // /regex/i
        Star, // *
    }
}
