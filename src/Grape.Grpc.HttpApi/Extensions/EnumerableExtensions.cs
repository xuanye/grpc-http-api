﻿using System;
using System.Collections.Generic;
using System.Text;

namespace System.Collections.Generic
{
    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            if (collection == null || action == null)
                return;

            foreach (var item in collection)
                action(item);
        }
    }
}
