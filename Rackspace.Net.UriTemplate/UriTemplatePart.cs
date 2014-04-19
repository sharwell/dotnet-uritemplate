﻿// Copyright (c) Rackspace, US Inc. All Rights Reserved. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Rackspace.Net
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Represents a single part of a decomposed URI Template.
    /// </summary>
    /// <threadsafety static="true" instance="false"/>
    /// <preliminary/>
    internal abstract class UriTemplatePart
    {
        /// <summary>
        /// Gets the type of this part.
        /// </summary>
        public abstract UriTemplatePartType Type
        {
            get;
        }

        /// <summary>
        /// Renders this <see cref="UriTemplatePart"/> to a <see cref="StringBuilder"/>, applying the
        /// specified <paramref name="parameters"/> as replacements for variables in the template.
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/> to render this part to.</param>
        /// <param name="parameters">A collection of parameters for replacing variable references in the template.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="builder"/> is <see langword="null"/>.</exception>
        public abstract void Render<T>(StringBuilder builder, IDictionary<string, T> parameters)
            where T : class;

        /// <summary>
        /// Determines if an ASCII character matches the <c>unreserved</c> pattern defined
        /// in RFC 6570.
        /// </summary>
        /// <param name="b">The ASCII character to test.</param>
        /// <returns><see langword="true"/> if <paramref name="b"/> is an <c>unreserved</c> character; otherwise, <see langword="false"/>.</returns>
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

        /// <summary>
        /// Determines if an ASCII character matches the <c>reserved</c> pattern defined
        /// in RFC 6570.
        /// </summary>
        /// <param name="b">The ASCII character to test.</param>
        /// <returns><see langword="true"/> if <paramref name="b"/> is a <c>reserved</c> character; otherwise, <see langword="false"/>.</returns>
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

        /// <summary>
        /// Encodes text for inclusion in a URI via an expansion.
        /// </summary>
        /// <param name="text">The text to encode.</param>
        /// <param name="allowReservedSet"><see langword="true"/> to allow <c>reserved</c> characters to pass through without percent-encoding; otherwise, <see langword="false"/> to percent-encode these characters.</param>
        /// <returns>The encoded text for inclusion in a URI.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="text"/> is <see langword="null"/>.</exception>
        protected static string EncodeReservedCharacters(string text, bool allowReservedSet)
        {
            if (text == null)
                throw new ArgumentNullException("text");

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
    }
}
