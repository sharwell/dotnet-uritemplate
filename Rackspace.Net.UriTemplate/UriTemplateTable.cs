namespace Rackspace.Net
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// A class that represents an associative set of <see cref="UriTemplate"/> objects.
    /// </summary>
    /// <threadsafety static="true" instance="true"/>
    /// <preliminary/>
    public class UriTemplateTable
    {
        /// <summary>
        /// This is the backing field for the <see cref="Templates"/> property.
        /// </summary>
        private readonly List<UriTemplate> _templates = new List<UriTemplate>();

        /// <summary>
        /// Gets a value indicating whether this <see cref="UriTemplateTable"/> is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a collection of URI templates stored in this table.
        /// </summary>
        /// <remarks>
        /// This collection may be modified as long as <see cref="IsReadOnly"/> is <c>false</c>.
        /// </remarks>
        public ICollection<UriTemplate> Templates
        {
            get
            {
                return _templates;
            }
        }

        /// <summary>
        /// Attempts to match a candidate <see cref="Uri"/> to the <see cref="UriTemplateTable"/>.
        /// </summary>
        /// <param name="candidate">The candidate URI.</param>
        /// <returns>A collection of <see cref="UriTemplateMatch"/> objects describing the successful match operations.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="candidate"/> is <c>null</c>.</exception>
        public ReadOnlyCollection<UriTemplateMatch> Match(Uri candidate)
        {
            if (candidate == null)
                throw new ArgumentNullException("candidate");

            List<UriTemplateMatch> matches = new List<UriTemplateMatch>();
            foreach (var template in Templates)
            {
                try
                {
                    UriTemplateMatch match = template.Match(candidate);
                    if (match != null)
                        matches.Add(match);
                }
                catch (NotSupportedException)
                {
                }
                catch (FormatException)
                {
                }
            }

            return matches.AsReadOnly();
        }
    }
}
