namespace Rackspace.Net
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    internal abstract class UriTemplatePart
    {
        public abstract UriTemplatePartType Type
        {
            get;
        }

        public abstract void Render(StringBuilder builder, IDictionary<string, object> parameters);

        public void BuildPattern(StringBuilder pattern, string groupName, ICollection<string> listVariables, ICollection<string> mapVariables)
        {
            pattern.Append("(?");
            if (!string.IsNullOrEmpty(groupName))
                pattern.Append('<').Append(groupName).Append('>');
            else
                pattern.Append(':');

            BuildPatternBody(pattern, listVariables, mapVariables);
            pattern.Append(')');
        }

        protected abstract void BuildPatternBody(StringBuilder pattern, ICollection<string> listVariables, ICollection<string> mapVariables);

        protected static bool IsUnreserved(byte b)
        {
            if (b >= 'a' && b <= 'z')
                return true;

            if (b >= 'A' && b <= 'Z')
                return true;

            if (b >= '0' && b <= '9')
                return true;

            switch ((char)b)
            {
                case '-':
                case '.':
                case '_':
                case '~':
                    return true;

                default:
                    return false;
            }
        }

        protected static bool IsReserved(byte b)
        {
            switch ((char)b)
            {
                // gen-delims
                case ':':
                case '/':
                case '?':
                case '#':
                case '[':
                case ']':
                case '@':
                    return true;

                // sub-delims
                case '!':
                case '$':
                case '&':
                case '\'':
                case '(':
                case ')':
                case '*':
                case '+':
                case ',':
                case ';':
                case '=':
                    return true;

                default:
                    return false;
            }
        }

        protected static string EncodeReservedCharacters(string text, bool allowReservedSet)
        {
            StringBuilder builder = new StringBuilder();
            byte[] encoded = Encoding.UTF8.GetBytes(text);
            foreach (byte b in encoded)
            {
                bool escape = !IsUnreserved(b);
                if (escape && allowReservedSet && IsReserved(b))
                    escape = false;

                if (escape)
                    builder.Append('%').Append(b.ToString("X2"));
                else
                    builder.Append((char)b);
            }

            return builder.ToString();
        }

        protected static string DecodeCharacters(string text)
        {
            byte[] data = new byte[text.Length];
            int length = 0;

            // the current position in text
            int position = 0;
            // the index of the current % character in text
            int index = -1;
            for (index = text.IndexOf('%', index + 1); index >= 0; index = text.IndexOf('%', index + 1))
            {
                while (position < index)
                {
                    data[length] = (byte)text[position];
                    length++;
                    position++;
                }

                string hex = text.Substring(index + 1, 2);
                int value = int.Parse(hex, NumberStyles.AllowHexSpecifier);
                data[length] = (byte)value;
                length++;
                position = index + 3;
            }

            while (position < text.Length)
            {
                data[length] = (byte)text[position];
                length++;
                position++;
            }

            return Encoding.UTF8.GetString(data, 0, length);
        }

        protected internal abstract KeyValuePair<VariableReference, object>[] Match(string text, ICollection<string> listVariables, ICollection<string> mapVariables);
    }
}
