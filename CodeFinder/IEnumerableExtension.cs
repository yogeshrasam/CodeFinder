﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeFinder
{
    public static class IEnumerableExtension
    {
        public static IEnumerable<IEnumerable<T>> MakeGroupsOf<T>(this IEnumerable<T> source, int count)
        {
            var grouping = new List<T>();
            foreach (var item in source)
            {
                grouping.Add(item);
                if (grouping.Count == count)
                {
                    yield return grouping;
                    grouping = new List<T>();
                }
            }

            if (grouping.Count != 0)
            {
                yield return grouping;
            }
        }
    }
}
