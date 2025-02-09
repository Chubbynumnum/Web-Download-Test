using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using TetraCreations.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class Test : MonoBehaviour
{
    [SerializeField] TMP_Text m_Text;

    async Awaitable GetDocumentAsync()
    {
        using (var client = new HttpClient())
        {
            // We'll use the GetAsync method to send  
            // a GET request to the specified URL 
            var response = await client.GetAsync("https://www.bible.com/bible/111/JHN.1.NIV");

            // If the response is successful, we'll 
            // interpret the response as XML 
            if (response.IsSuccessStatusCode)
            {
                var xml = await response.Content.ReadAsStringAsync();

                // We can then use the LINQ to XML API to query the XML 
                var doc = XDocument.Parse(xml);

                var strings = doc.Descendants("main").SelectMany(x => x.Descendants("div")).ToList();

                // Let's query the XML to get all of the <div> elements 
                var titles = from el in doc.Descendants("div")
                             select el.Value;

                foreach (var title in strings) 
                {
                    Debug.Log("GetDocumentAsync");
                    Debug.Log(title.ToString());
                } 
            }
        }
    }

    IEnumerator GetText()
    {
        UnityWebRequest www = UnityWebRequest.Get("https://www.bible.com/bible/111/JHN.1.NIV");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            //The string that indicates the start of the chapter and also the start of each paragraph (its kinda confusing the way the website works)
            string toBeSearched = "<div class=\"ChapterContent_p__dVKHb\">";
            int ix = www.downloadHandler.text.IndexOf(toBeSearched);
            //if the string cant be found then break cus somthing went wrong
            if (ix == -1) { yield break; }
            //extract the chapter as HTML in a string
            string code = www.downloadHandler.text.Substring(ix + toBeSearched.Length);
            string[] lines = code.Split(new[] { toBeSearched }, StringSplitOptions.None); // split chapter into paragraphs
            lines[^1] = lines[^1].Split(new[] { "</div>" }, StringSplitOptions.None)[0]; // remove last paragraph as that is bottom of website stuff

            string chaptCode = "JHN.1";// the code of the chapter we a searching
            List<string> versesAdded = new(); // lsit of all the verses
            int max = 10; // how many verses to get (always one less than the number you put NOTE: i should change this)


            for (int i = 0; i < max; i++) // loop to check for all the verses we want
            {
                foreach (string line in lines)
                {
                    //The string that indicates where paragraph that contains the verse (and other verse as well for some reason) start from
                    string verseSpan = String.Format("<span data-usfm=\"{0}.{1}\" class=\"ChapterContent_verse__57FIw\">", chaptCode, i);
                    //The string that indicates where the verse we are looking for starts from inside the paragraph of verses
                    string verseSpanLable = String.Format("<span class=\"ChapterContent_label__R2PLt\">{0}</span>", i);//verse identifyer
                    //The string that indicates where the next verse starts from                                                                                                    //
                    string nextVerseSpanLable = String.Format("<span class=\"ChapterContent_label__R2PLt\">{0}</span>", i + 1);//verse identifyer 

                    string CommentKey = "<span class=\"ChapterContent_note__YlDW0\">";//use to finde comments to remove 
                    //The string that indicates where the actual text for a verse begins
                    string TextKey = "<span class=\"ChapterContent_content__RrUqA\">";//Class that holds text in a verse 

                    if (line.IndexOf(verseSpan) == -1) // if the paragraph couldnt be found
                    {
                        Debug.Log(verseSpan);
                        continue; //next chapter doesnt exsist
                    }
                    else
                    {
                        //The index of where the verse we are looking for starts from inside the paragraph of verses
                        int NextVerseindex = line.Substring(line.IndexOf(verseSpan)).IndexOf(nextVerseSpanLable);
                        //The index that indicates where the next verse starts from
                        int CurrentVerseindex = line.Substring(line.IndexOf(verseSpan)).IndexOf(verseSpanLable);

                        Debug.Log("Current verse index = " + CurrentVerseindex + " Next verse index = " + NextVerseindex);

                        if(CurrentVerseindex < 0) { continue; }

                        var txt = line.Substring(line.IndexOf(verseSpan)); // raw text with html that contains the required verse
                        if (NextVerseindex > 0)
                        {
                            // if there is a next verse the exstract the text up to the next verse. 
                            string filtertVerse = txt.Substring(CurrentVerseindex, NextVerseindex - CurrentVerseindex); // raw html verse without other verses
                            string verseNoComments = RemoveComments(filtertVerse, CommentKey, TextKey);//html verse without comments
                            versesAdded.Add(HTMLToText(verseNoComments));
                        }
                        else
                        {
                            // if there is no next verse then exstract the text to the end (This should be fine cuz the buttom of page stuff was removed)
                            string filtertVerse = txt.Substring(CurrentVerseindex); // raw html verse without unessary tags
                            string verseNoComments = RemoveComments(filtertVerse, CommentKey, TextKey);//html verse without comments
                            versesAdded.Add(HTMLToText(verseNoComments));
                        }

                    }
                }
            }
            //print text to screen
            m_Text.text = RemoveDuplicates(versesAdded);  
        }
    }
    /// <summary>
    /// Removes the part of the HTML that contains comments.
    /// </summary>
    /// <param name="filterdVerse">Raw HTML verse without other verses</param>
    /// <param name="commentKey">The HTML span that contains the comments</param>
    /// <param name="textKey">Class that holds raw text in a verse. This is used to find the end of the comment</param>
    /// <returns>A HTML string without the embedded comments</returns>
    string RemoveComments(string filterdVerse, string commentKey, string textKey)
    {
        int commentStartIndex = filterdVerse.IndexOf(commentKey);
        int commentEndIndex = FindIndexAfter(textKey, commentKey, filterdVerse);

        if(commentStartIndex == -1 || commentEndIndex == -1) // if therse no comments 
        {
            return filterdVerse; //return the verse
        }
        int count = commentEndIndex - commentStartIndex; // how many characters to remove (Cant be 0)
        if (count < 0) { count = 0; }
        return filterdVerse.Remove(commentStartIndex, count);
    }
    /// <summary>
    /// Finds the index of the first instance a string after a specific string.
    /// </summary>
    /// <param name="findIndexOf">The string to find the index of</param>
    /// <param name="searchAfter">The string that indicates where the search should start from</param>
    /// <param name="searchIn">The string to search for the <paramref name="findIndexOf" /> string in</param>
    /// <returns>The index of the string. Is -1 if not found</returns>
    int FindIndexAfter(string findIndexOf, string searchAfter, string searchIn)
    {
        int searchAfterIndex = searchIn.IndexOf(searchAfter);
        if(searchAfterIndex == -1) { return -1; }
        string startRemoved = searchIn.Substring(searchAfterIndex);// only contains charaters after the searchAfterIndex
        int ind = startRemoved.IndexOf(findIndexOf);
        //add the indexes that were removed
        return ind + searchAfterIndex;
    }
    /// <summary>
    /// Combines a list of string together while removeing duplicates.
    /// (Technically just combines now)
    /// </summary>
    /// <param name="strings">List of a strings to be combined</param>
    /// <returns>A single string containing all of the other strings</returns>
    string RemoveDuplicates(List<string> strings)
    {
        var output = new StringBuilder();

        for (int i = 0; i < strings.Count; i++)
        {
            string stringInstance = strings[i];
            //string NextStringInstance = strings[Math.Clamp(i + 1, 0, strings.Count - 1)];

            output.AppendJoin(" ", stringInstance);
        }

        return output.ToString();
    }
    /// <summary>
    /// Exstracts text from HTML code
    /// </summary>
    /// <param name="HTMLCode">The HTML to get text from</param>
    /// <returns>String containing Text from the HTML without the HTML code</returns>
    public string HTMLToText(string HTMLCode)
    {
        // Remove new lines since they are not visible in HTML  
        HTMLCode = HTMLCode.Replace("\n", " ");
        // Remove tab spaces  
        HTMLCode = HTMLCode.Replace("\t", " ");
        // Remove multiple white spaces from HTML  
        HTMLCode = Regex.Replace(HTMLCode, "\\s+", " ");
        // Remove HEAD tag  
        HTMLCode = Regex.Replace(HTMLCode, "<head.*?</head>", ""
                            , RegexOptions.IgnoreCase | RegexOptions.Singleline);
        // Remove any JavaScript  
        HTMLCode = Regex.Replace(HTMLCode, "<script.*?</script>", ""
          , RegexOptions.IgnoreCase | RegexOptions.Singleline);
        // Replace special characters like &, <, >, " etc.  
        StringBuilder sbHTML = new StringBuilder(HTMLCode);
        // Note: There are many more special characters, these are just  
        // most common. You can add new characters in this arrays if needed  
        string[] OldWords = {"&nbsp;", "&amp;", "&quot;", "&lt;",
        "&gt;", "&reg;", "&copy;", "&bull;", "&trade;","&#39;"};
        string[] NewWords = { " ", "&", "\"", "<", ">", "®", "©", "•", "™", "\'" };
        for (int i = 0; i < OldWords.Length; i++)
        {
            sbHTML.Replace(OldWords[i], NewWords[i]);
        }
        // Check if there are line breaks (<br>) or paragraph (<p>)  
        sbHTML.Replace("<br>", "\n<br>");
        sbHTML.Replace("<br ", "\n<br ");
        sbHTML.Replace("<p ", "\n<p ");
        // Finally, remove all HTML tags and return plain text  
        return Regex.Replace(
          sbHTML.ToString(), "<[^>]*>", "");
    }

    [Button(nameof(FetchText))]
    public void FetchText()
    {
        if (m_Text != null) 
        {
            _ = StartCoroutine(GetText());
            _ = GetDocumentAsync();
        }
    }
}
