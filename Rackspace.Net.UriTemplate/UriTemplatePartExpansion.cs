﻿namespace Rackspace.Net
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using IDictionary = System.Collections.IDictionary;
    using IEnumerable = System.Collections.IEnumerable;

    internal abstract class UriTemplatePartExpansion : UriTemplatePart
    {
        protected const string UnreservedCharacterPattern = @"(?:[a-zA-Z0-9._~-]|" + UriTemplate.PctEncodedPattern + ")";

        protected const string ReservedCharacterPattern = @"(?:[:/?#[\]@!$&'()*+,;=]" + @")";

        private readonly VariableReference[] _variables;

        public UriTemplatePartExpansion(IEnumerable<VariableReference> variables)
        {
            if (variables == null)
                throw new ArgumentNullException("variables");

            _variables = variables.ToArray();
        }

        public ReadOnlyCollection<VariableReference> Variables
        {
            get
            {
                return new ReadOnlyCollection<VariableReference>(_variables);
            }
        }

        public override sealed void Render(StringBuilder builder, IDictionary<string, object> parameters)
        {
            bool added = false;
            for (int i = 0; i < _variables.Length; i++)
            {
                object result;
                if (!parameters.TryGetValue(_variables[i].Name, out result) || result == null)
                    continue;

                IDictionary dictionary = result as IDictionary;
                if (dictionary != null)
                {
                    if (_variables[i].Prefix != null)
                        throw new InvalidOperationException(string.Format("Cannot apply prefix modifier to associative map value '{0}'", _variables[i].Name));

                    RenderDictionary(builder, _variables[i], dictionary, !added);
                }
                else
                {
                    IEnumerable enumerable = result as IEnumerable;
                    if (enumerable != null && !(result is string))
                    {
                        if (_variables[i].Prefix != null)
                            throw new InvalidOperationException(string.Format("Cannot apply prefix modifier to composite value '{0}'", _variables[i].Name));

                        RenderEnumerable(builder, _variables[i], enumerable, !added);
                    }
                    else
                    {
                        RenderElement(builder, _variables[i], result, !added);
                    }
                }

                added = true;
            }
        }

        protected override sealed void BuildPatternBody(StringBuilder pattern, ICollection<string> listVariables, ICollection<string> mapVariables)
        {
            foreach (VariableReference variable in Variables)
            {
                if (variable.Prefix != null)
                {
                    if (listVariables.Contains(variable.Name))
                        throw new InvalidOperationException("Cannot treat a variable with a prefix modifier as a list.");
                    if (mapVariables.Contains(variable.Name))
                        throw new InvalidOperationException("Cannot treat a variable with a prefix modifier as an associative map.");
                }
            }

            BuildPatternBodyImpl(pattern, listVariables, mapVariables);
        }

        protected abstract void BuildPatternBodyImpl(StringBuilder pattern, ICollection<string> listVariables, ICollection<string> mapVariables);

        protected static void AppendText(StringBuilder builder, VariableReference variable, string value, bool escapeReserved)
        {
            string text = value;
            if (variable.Prefix != null && text.Length > variable.Prefix)
                text = text.Substring(0, variable.Prefix.Value);

            text = EncodeReservedCharacters(text, !escapeReserved);
            builder.Append(text);
        }

        protected abstract void RenderDictionary(StringBuilder builder, VariableReference variable, IDictionary variableValue, bool first);

        protected abstract void RenderEnumerable(StringBuilder builder, VariableReference variable, IEnumerable variableValue, bool first);

        protected abstract void RenderElement(StringBuilder builder, VariableReference variable, object variableValue, bool first);
    }
}
