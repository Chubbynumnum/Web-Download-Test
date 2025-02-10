using System.Collections.Generic;
using System;
using System.Linq;

public static class StringExtensions
{
    /// <summary>
    ///     Returns a string array that contains the substrings in this instance that are delimited by specified indexes.
    /// </summary>
    /// <param name="source">The original string.</param>
    /// <param name="index">An index that delimits the substrings in this string.</param>
    /// <returns>An array whose elements contain the substrings in this instance that are delimited by one or more indexes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="index" /> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An <paramref name="index" /> is less than zero or greater than the length of this instance.</exception>
    public static string[] SplitAt(this string source, params int[] index)
    {
        index = index.Distinct().OrderBy(x => x).ToArray();
        string[] output = new string[index.Length + 1];
        int pos = 0;

        for (int i = 0; i < index.Length; pos = index[i++])
            output[i] = source.Substring(pos, index[i] - pos);

        output[index.Length] = source.Substring(pos);
        return output;
    }

    public static List<int> AllIndexesOf(this string str, string value)
    {
        if (String.IsNullOrEmpty(value))
            throw new ArgumentException("the string to find may not be empty", "value");
        List<int> indexes = new List<int>();
        for (int index = 0; ; index += value.Length)
        {
            index = str.IndexOf(value, index);
            if (index == -1)
                return indexes;
            indexes.Add(index);
        }
    }
}