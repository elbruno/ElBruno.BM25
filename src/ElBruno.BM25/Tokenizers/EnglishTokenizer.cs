namespace ElBruno.BM25.Tokenizers;

using System;
using System.Collections.Generic;
using System.Linq;
using ElBruno.BM25;

/// <summary>
/// English-aware tokenizer with Porter stemming.
/// Applies stemming to normalize morphological variants (e.g., "authentication", "authenticate", "authenticating").
/// </summary>
public class EnglishTokenizer : ITokenizer
{
    /// <summary>
    /// Gets the name of this tokenizer.
    /// </summary>
    public string Name => "English";

    /// <summary>
    /// Tokenizes English text with stemming.
    /// </summary>
    /// <param name="text">The input text to tokenize.</param>
    /// <returns>A list of stemmed terms.</returns>
    public List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new();
        
        var terms = new List<string>();
        var chars = text.ToLowerInvariant().ToCharArray();
        var currentTerm = new System.Text.StringBuilder();

        foreach (var c in chars)
        {
            if (char.IsLetterOrDigit(c))
            {
                currentTerm.Append(c);
            }
            else if (currentTerm.Length > 0)
            {
                var stemmed = PorterStem(currentTerm.ToString());
                if (!string.IsNullOrEmpty(stemmed))
                    terms.Add(stemmed);
                currentTerm.Clear();
            }
        }

        if (currentTerm.Length > 0)
        {
            var stemmed = PorterStem(currentTerm.ToString());
            if (!string.IsNullOrEmpty(stemmed))
                terms.Add(stemmed);
        }

        return terms;
    }

    /// <summary>
    /// Normalizes a term by lowercasing and applying Porter stemming.
    /// </summary>
    /// <param name="term">The term to normalize.</param>
    /// <returns>The stemmed term.</returns>
    public string Normalize(string term)
    {
        return PorterStem(term?.ToLowerInvariant() ?? string.Empty);
    }

    private static string PorterStem(string word)
    {
        if (word.Length <= 2) return word;

        word = Step1a(word);
        word = Step1b(word);
        word = Step1c(word);
        word = Step2(word);
        word = Step3(word);
        word = Step4(word);
        word = Step5(word);

        return word;
    }

    private static string Step1a(string word)
    {
        if (word.EndsWith("sses")) return word.Substring(0, word.Length - 2);
        if (word.EndsWith("ies")) return word.Substring(0, word.Length - 3) + "i";
        if (word.EndsWith("ss")) return word;
        if (word.EndsWith("s")) return word.Substring(0, word.Length - 1);
        return word;
    }

    private static string Step1b(string word)
    {
        if (word.EndsWith("eed"))
        {
            var stem = word.Substring(0, word.Length - 3);
            return Measure(stem) > 0 ? stem + "ee" : word;
        }
        if ((word.EndsWith("ed") || word.EndsWith("ing")) && ContainsVowel(word.Substring(0, word.Length - 3)))
        {
            word = word.EndsWith("ed") ? word.Substring(0, word.Length - 2) : word.Substring(0, word.Length - 3);
            if (word.EndsWith("at") || word.EndsWith("bl") || word.EndsWith("iz")) return word + "e";
            if (word.Length >= 2 && word[word.Length - 1] == word[word.Length - 2] && 
                "lsz".Contains(word[word.Length - 1])) return word.Substring(0, word.Length - 1);
            if (Measure(word) == 1 && IsShortSyllable(word)) return word + "e";
            return word;
        }
        return word;
    }

    private static string Step1c(string word)
    {
        if (word.Length >= 3 && (word.EndsWith("y") || word.EndsWith("Y")) && ContainsVowel(word.Substring(0, word.Length - 1)))
            return word.Substring(0, word.Length - 1) + "i";
        return word;
    }

    private static string Step2(string word)
    {
        if (word.Length > 3)
        {
            if (word.EndsWith("ational") && Measure(word.Substring(0, word.Length - 7)) > 0) return word.Substring(0, word.Length - 7) + "e";
            if (word.EndsWith("tional") && Measure(word.Substring(0, word.Length - 6)) > 0) return word.Substring(0, word.Length - 6);
            if (word.EndsWith("enci") && Measure(word.Substring(0, word.Length - 4)) > 0) return word.Substring(0, word.Length - 4) + "e";
            if (word.EndsWith("anci") && Measure(word.Substring(0, word.Length - 4)) > 0) return word.Substring(0, word.Length - 4) + "e";
            if (word.EndsWith("izer") && Measure(word.Substring(0, word.Length - 4)) > 0) return word.Substring(0, word.Length - 4) + "e";
            if (word.EndsWith("bli") && Measure(word.Substring(0, word.Length - 3)) > 0) return word.Substring(0, word.Length - 3) + "e";
            if (word.EndsWith("alli") && Measure(word.Substring(0, word.Length - 4)) > 0) return word.Substring(0, word.Length - 4);
            if (word.EndsWith("entli") && Measure(word.Substring(0, word.Length - 5)) > 0) return word.Substring(0, word.Length - 5);
            if (word.EndsWith("eli") && Measure(word.Substring(0, word.Length - 3)) > 0) return word.Substring(0, word.Length - 3);
            if (word.EndsWith("ousli") && Measure(word.Substring(0, word.Length - 5)) > 0) return word.Substring(0, word.Length - 5);
            if (word.EndsWith("ization") && Measure(word.Substring(0, word.Length - 7)) > 0) return word.Substring(0, word.Length - 7) + "e";
            if (word.EndsWith("ation") && Measure(word.Substring(0, word.Length - 5)) > 0) return word.Substring(0, word.Length - 5) + "e";
            if (word.EndsWith("ator") && Measure(word.Substring(0, word.Length - 4)) > 0) return word.Substring(0, word.Length - 4) + "e";
            if (word.EndsWith("alism") && Measure(word.Substring(0, word.Length - 5)) > 0) return word.Substring(0, word.Length - 5);
            if (word.EndsWith("iveness") && Measure(word.Substring(0, word.Length - 7)) > 0) return word.Substring(0, word.Length - 7);
            if (word.EndsWith("fulness") && Measure(word.Substring(0, word.Length - 7)) > 0) return word.Substring(0, word.Length - 7);
            if (word.EndsWith("ousness") && Measure(word.Substring(0, word.Length - 7)) > 0) return word.Substring(0, word.Length - 7);
            if (word.EndsWith("aliti") && Measure(word.Substring(0, word.Length - 5)) > 0) return word.Substring(0, word.Length - 5);
            if (word.EndsWith("iviti") && Measure(word.Substring(0, word.Length - 5)) > 0) return word.Substring(0, word.Length - 5);
            if (word.EndsWith("biliti") && Measure(word.Substring(0, word.Length - 6)) > 0) return word.Substring(0, word.Length - 6);
            if (word.EndsWith("logi") && Measure(word.Substring(0, word.Length - 4)) > 0) return word.Substring(0, word.Length - 4) + "e";
        }
        return word;
    }

    private static string Step3(string word)
    {
        if (word.Length > 3)
        {
            if (word.EndsWith("icate") && Measure(word.Substring(0, word.Length - 5)) > 0) return word.Substring(0, word.Length - 5) + "ic";
            if (word.EndsWith("ative") && Measure(word.Substring(0, word.Length - 5)) > 0) return word.Substring(0, word.Length - 5);
            if (word.EndsWith("alize") && Measure(word.Substring(0, word.Length - 5)) > 0) return word.Substring(0, word.Length - 5);
        }
        return word;
    }

    private static string Step4(string word)
    {
        if (word.Length > 4)
        {
            if (word.EndsWith("al") && Measure(word.Substring(0, word.Length - 2)) > 1) return word.Substring(0, word.Length - 2);
            if (word.EndsWith("ance") && Measure(word.Substring(0, word.Length - 4)) > 1) return word.Substring(0, word.Length - 4);
            if (word.EndsWith("ence") && Measure(word.Substring(0, word.Length - 4)) > 1) return word.Substring(0, word.Length - 4);
            if (word.EndsWith("er") && Measure(word.Substring(0, word.Length - 2)) > 1) return word.Substring(0, word.Length - 2);
            if (word.EndsWith("ic") && Measure(word.Substring(0, word.Length - 2)) > 1) return word.Substring(0, word.Length - 2);
            if (word.EndsWith("able") && Measure(word.Substring(0, word.Length - 4)) > 1) return word.Substring(0, word.Length - 4);
            if (word.EndsWith("ible") && Measure(word.Substring(0, word.Length - 4)) > 1) return word.Substring(0, word.Length - 4);
            if (word.EndsWith("ant") && Measure(word.Substring(0, word.Length - 3)) > 1) return word.Substring(0, word.Length - 3);
            if (word.EndsWith("ement") && Measure(word.Substring(0, word.Length - 5)) > 1) return word.Substring(0, word.Length - 5);
            if (word.EndsWith("ment") && Measure(word.Substring(0, word.Length - 4)) > 1) return word.Substring(0, word.Length - 4);
            if (word.EndsWith("ent") && Measure(word.Substring(0, word.Length - 3)) > 1) return word.Substring(0, word.Length - 3);
            if (word.EndsWith("ion") && word.Length > 3 && "st".Contains(word[word.Length - 4]) && Measure(word.Substring(0, word.Length - 3)) > 1)
                return word.Substring(0, word.Length - 3);
            if (word.EndsWith("ou") && Measure(word.Substring(0, word.Length - 2)) > 1) return word.Substring(0, word.Length - 2);
            if (word.EndsWith("ism") && Measure(word.Substring(0, word.Length - 3)) > 1) return word.Substring(0, word.Length - 3);
            if (word.EndsWith("ate") && Measure(word.Substring(0, word.Length - 3)) > 1) return word.Substring(0, word.Length - 3);
            if (word.EndsWith("iti") && Measure(word.Substring(0, word.Length - 3)) > 1) return word.Substring(0, word.Length - 3);
            if (word.EndsWith("ous") && Measure(word.Substring(0, word.Length - 3)) > 1) return word.Substring(0, word.Length - 3);
            if (word.EndsWith("ive") && Measure(word.Substring(0, word.Length - 3)) > 1) return word.Substring(0, word.Length - 3);
            if (word.EndsWith("ize") && Measure(word.Substring(0, word.Length - 3)) > 1) return word.Substring(0, word.Length - 3);
        }
        return word;
    }

    private static string Step5(string word)
    {
        if (word.EndsWith("e"))
        {
            var stem = word.Substring(0, word.Length - 1);
            var m = Measure(stem);
            if (m > 1 || (m == 1 && !IsShortSyllable(stem)))
                return stem;
        }
        if (word.EndsWith("ll") && word.Length > 1 && Measure(word.Substring(0, word.Length - 1)) > 1)
            return word.Substring(0, word.Length - 1);
        return word;
    }

    private static int Measure(string word)
    {
        if (string.IsNullOrEmpty(word)) return 0;
        int count = 0;
        bool inVowel = false;
        foreach (var c in word)
        {
            bool isVowel = "aeiou".Contains(c);
            if (isVowel && !inVowel) count++;
            inVowel = isVowel;
        }
        return count;
    }

    private static bool ContainsVowel(string word)
    {
        return word.Any(c => "aeiou".Contains(c));
    }

    private static bool IsShortSyllable(string word)
    {
        if (word.Length < 3) return false;
        bool v = "aeiou".Contains(word[word.Length - 2]);
        bool c = !v && word.Length >= 2;
        return c && v && !v && "wxY".Contains(word[word.Length - 1]);
    }
}
