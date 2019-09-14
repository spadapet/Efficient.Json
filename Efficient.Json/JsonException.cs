using System;
using System.Globalization;
using Efficient.Json.Tokens;

namespace Efficient.Json
{
    /// <summary>
    /// This is the only type of exception directly thrown from this JSON component
    /// </summary>
    public class JsonException : Exception
    {
        internal FullToken Token { get; }
        public bool HasToken => this.Token.Type != TokenType.None;
        public int TokenLine => this.Token.Line;
        public int TokenColumn => this.Token.Column;
        public string TokenText => this.Token.EncodedText;

        public JsonException()
            : this(FullToken.Empty, string.Empty, null)
        {
        }

        public JsonException(string message)
            : this(FullToken.Empty, message, null)
        {
        }

        public JsonException(string message, Exception innerException)
            : this(FullToken.Empty, message, innerException)
        {
        }

        internal JsonException(FullToken token, string message, Exception innerException = null)
            : base(message ?? innerException?.Message ?? string.Empty, innerException)
        {
            this.Token = token;
        }

        internal static JsonException New(string message, params object[] args)
        {
            return new JsonException(string.Format(CultureInfo.CurrentCulture, message, args));
        }

        internal static JsonException WrongType(object actualType, object expectedType)
        {
            return new JsonException(string.Format(CultureInfo.CurrentCulture, Resources.Value_WrongType, actualType.ToString(), expectedType.ToString()));
        }

        internal static void Wrap(Action action)
        {
            try
            {
                action();
            }
            catch (JsonException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new JsonException(null, ex);
            }
        }

        internal static T Wrap<T>(Func<T> action)
        {
            try
            {
                return action();
            }
            catch (JsonException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new JsonException(null, ex);
            }
        }
    }
}
