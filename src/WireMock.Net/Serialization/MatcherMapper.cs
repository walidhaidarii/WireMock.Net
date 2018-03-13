﻿using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using WireMock.Admin.Mappings;
using WireMock.Matchers;

namespace WireMock.Serialization
{
    internal static class MatcherMapper
    {
        public static MatcherModel[] Map([CanBeNull] IEnumerable<IMatcher> matchers)
        {
            return matchers?.Select(Map).Where(x => x != null).ToArray();
        }

        public static MatcherModel Map([CanBeNull] IMatcher matcher)
        {
            if (matcher == null)
            {
                return null;
            }

            IStringMatcher stringMatcher = matcher as IStringMatcher;
            string[] patterns = stringMatcher != null ? stringMatcher.GetPatterns() : new string[0];

            return new MatcherModel
            {
                Name = matcher.GetName(),
                Pattern = patterns.Length == 1 ? patterns.First() : null,
                Patterns = patterns.Length > 1 ? patterns : null
            };
        }
    }
}
