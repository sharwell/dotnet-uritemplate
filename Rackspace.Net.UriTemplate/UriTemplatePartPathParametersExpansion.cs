﻿namespace Rackspace.Net
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using DictionaryEntry = System.Collections.DictionaryEntry;
    using IDictionary = System.Collections.IDictionary;
    using IEnumerable = System.Collections.IEnumerable;

    /// <summary>
    /// Represents a URI Template expression of the form <c>{;x,y}</c>.
    /// </summary>
    internal sealed class UriTemplatePartPathParametersExpansion : UriTemplatePartExpansion
    {
        public UriTemplatePartPathParametersExpansion(IEnumerable<VariableReference> variables)
            : base(variables)
        {
        }

        public override UriTemplatePartType Type
        {
            get
            {
                return UriTemplatePartType.PathParameters;
            }
        }

        protected override void BuildPatternBodyImpl(StringBuilder pattern, ICollection<string> listVariables, ICollection<string> mapVariables)
        {
            string keyFormat;
            int mapVariableCount = Variables.Count(i => mapVariables.Contains(i.Name));
            if (mapVariableCount > 0)
            {
                if (mapVariableCount > 1)
                    throw new NotSupportedException("Matching multiple composite associative map variables in a path parameter expansion is not supported");

                // allows any names for path parameters
                keyFormat = UnreservedCharacterPattern + "*";
            }
            else
            {
                // only allows specific names for path parameters
                StringBuilder allowedNamesBuilder = new StringBuilder();
                allowedNamesBuilder.Append("(?:");
                for (int i = 0; i < Variables.Count; i++)
                {
                    if (i > 0)
                        allowedNamesBuilder.Append("|");

                    allowedNamesBuilder.Append(Regex.Escape(Variables[i].Name));
                }

                allowedNamesBuilder.Append(")");
                keyFormat = allowedNamesBuilder.ToString();
            }

            string valueFormat = UnreservedCharacterPattern + "*";
            bool hasListOrMapVariable = Variables.Any(i => listVariables.Contains(i.Name) || mapVariables.Contains(i.Name));
            if (hasListOrMapVariable)
                valueFormat = valueFormat + "(?:" + Regex.Escape(",") + valueFormat + ")*";

            string elementFormat = keyFormat + "(?:" + Regex.Escape("=") + valueFormat + ")?";

            pattern.Append("(?:");

            pattern.Append("(?:");
            pattern.Append(Regex.Escape(";"));
            pattern.Append(elementFormat);
            pattern.Append(")*");

            pattern.Append(")?");
        }

        protected internal override KeyValuePair<VariableReference, object>[] Match(string text, ICollection<string> listVariables, ICollection<string> mapVariables)
        {
            if (string.IsNullOrEmpty(text))
                return new KeyValuePair<VariableReference, object>[0];

            if (text[0] != ';')
                throw new FormatException("The specified text is not a valid path parameter expansion");

            text = text.Substring(1);

            string[] parameters = text.Split(';');
            Dictionary<string, List<string>> extracted = new Dictionary<string, List<string>>();
            foreach (string parameter in parameters)
            {
                int eqIndex = parameter.IndexOf('=');
                string key;
                string value;
                if (eqIndex < 0)
                {
                    key = parameter;
                    value = string.Empty;
                }
                else
                {
                    key = parameter.Substring(0, eqIndex);
                    value = parameter.Substring(eqIndex + 1);
                }

                List<string> values;
                if (!extracted.TryGetValue(key, out values))
                {
                    values = new List<string>();
                    extracted.Add(key, values);
                }

                values.Add(value);
            }

            List<KeyValuePair<VariableReference, object>> bindings = new List<KeyValuePair<VariableReference, object>>();
            foreach (VariableReference variable in Variables)
            {
                if (variable.Composite)
                {
                    if (listVariables.Contains(variable.Name))
                    {
                        List<string> values;
                        if (!extracted.TryGetValue(variable.Name, out values))
                            continue;

                        if (values.Any(value => value.IndexOf(',') >= 0))
                            throw new FormatException();

                        bindings.Add(new KeyValuePair<VariableReference, object>(variable, values.ToArray().ConvertAll(DecodeCharacters)));
                    }
                    else if (mapVariables.Contains(variable.Name))
                    {
                        Dictionary<string, string> map = new Dictionary<string, string>();
                        foreach (KeyValuePair<string, List<string>> pair in extracted)
                        {
                            if (Variables.Any(v => v.Name.Equals(pair.Key)))
                                continue;

                            if (pair.Value.Count != 1)
                                throw new FormatException();

                            map.Add(DecodeCharacters(pair.Key), DecodeCharacters(pair.Value[0]));
                        }

                        bindings.Add(new KeyValuePair<VariableReference, object>(variable, map));
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    List<string> values;
                    if (!extracted.TryGetValue(variable.Name, out values))
                        continue;

                    if (values.Count > 1)
                        throw new FormatException(string.Format("Non-composite variable '{0}' can only specified once in the path parameter string.", variable.Name));

                    if (listVariables.Contains(variable.Name))
                    {
                        string[] listElements = values[0].Split(',');
                        bindings.Add(new KeyValuePair<VariableReference, object>(variable, listElements.ConvertAll(DecodeCharacters)));
                    }
                    else if (mapVariables.Contains(variable.Name))
                    {
                        string[] mapElements = values[0].Split(',');
                        if ((mapElements.Length % 2) != 0)
                            throw new FormatException();

                        Dictionary<string, string> map = new Dictionary<string, string>();
                        for (int i = 0; i < mapElements.Length; i += 2)
                            map.Add(DecodeCharacters(mapElements[i]), DecodeCharacters(mapElements[i + 1]));

                        bindings.Add(new KeyValuePair<VariableReference, object>(variable, map));
                    }
                    else
                    {
                        if (values[0].IndexOf(',') >= 0)
                            throw new FormatException();

                        string decodedValue = DecodeCharacters(values[0]);
                        if (variable.Prefix < decodedValue.Length)
                            throw new FormatException(string.Format("Variable '{0}' has a maximum length of {1}", variable.Name, variable.Prefix));

                        bindings.Add(new KeyValuePair<VariableReference, object>(variable, decodedValue));
                    }
                }
            }

            return bindings.ToArray();
        }

        protected override void RenderElement(StringBuilder builder, VariableReference variable, object variableValue, bool first)
        {
            RenderElement(builder, variable, variableValue, first, true);
        }

        protected override void RenderEnumerable(StringBuilder builder, VariableReference variable, IEnumerable variableValue, bool first)
        {
            bool firstElement = true;
            foreach (object value in variableValue)
            {
                if (value == null)
                    continue;

                RenderElement(builder, variable, value, first, firstElement);
                firstElement = false;
            }
        }

        protected override void RenderDictionary(StringBuilder builder, VariableReference variable, IDictionary variableValue, bool first)
        {
            bool firstElement = true;
            foreach (DictionaryEntry entry in variableValue)
            {
                if (variable.Composite)
                {
                    builder.Append(';');
                    AppendText(builder, variable, entry.Key.ToString(), true);
                    builder.Append('=');
                    AppendText(builder, variable, entry.Value.ToString(), true);
                }
                else
                {
                    RenderElement(builder, variable, entry.Key, first, firstElement);
                    RenderElement(builder, variable, entry.Value, first, false);
                }

                firstElement = false;
            }
        }

        private void RenderElement(StringBuilder builder, VariableReference variable, object variableValue, bool firstVariable, bool firstElement)
        {
            if (builder == null)
                throw new ArgumentNullException("builder");
            if (variableValue == null)
                throw new ArgumentNullException("variableValue");

            if (firstElement || variable.Composite)
                builder.Append(";").Append(variable.Name);
            else if (!firstElement)
                builder.Append(',');

            string text = variableValue.ToString();
            if ((firstElement || variable.Composite) && !string.IsNullOrEmpty(text))
                builder.Append('=');

            AppendText(builder, variable, text, true);
        }

        public override string ToString()
        {
            return string.Format("{{;{0}}}", string.Join(",", Variables.Select(i => i.Name).ToArray()));
        }
    }
}
