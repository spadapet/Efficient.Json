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
        AtRef, // @ (before @.foo or @[...])
        Bang, // !
        BangEqual, // !=
        CloseBracket, // ]
        CloseParen, // )
        Colon, // :
        Comma, // ,
        DollarRef, // $ (before $.foo or $[...])
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
