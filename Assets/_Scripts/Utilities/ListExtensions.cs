using System;
using System.Collections.Generic;

namespace Game.Utils
{
    public static class ListExtensions
    {
        private static System.Random random = new System.Random();

        public static T GetRandomElement<T>(this List<T> list)
        {
            if (list == null || list.Count == 0)
            {
                throw new ArgumentException("The list is empty or null");
            }

            int index = random.Next(list.Count);
            return list[index];
        }
    }
}