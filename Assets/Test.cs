using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TetraCreations.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class Test : MonoBehaviour
{
    [SerializeField] TMP_Text m_Text;

    [SerializeField] Books Book;
    
    [SerializeField, Min(1)] int Chapter = 1;

    [SerializeField, MinMaxSlider(1, 150)] Vector2Int verses;

    IEnumerator GetText(string book, int chpt, Vector2Int ver)
    {
        UnityWebRequest www = UnityWebRequest.Get(String.Format("https://www.bible.com/bible/111/{0}.{1}.NIV", book, chpt));
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            m_Text.text = "NOT FOUND";
        }
        else
        {
            //The string that indicates the start of the chapter and also the start of each paragraph (its kinda confusing the way the website works)
            string toBeSearched = String.Format("<div data-usfm=\"{0}.{1}\" class=\"ChapterContent_chapter__uvbXo\">", book, chpt);
            int ix = www.downloadHandler.text.IndexOf(toBeSearched);
            //if the string cant be found then break cus somthing went wrong
            if (ix == -1) 
            {
                m_Text.text = "NOT FOUND";
                yield break; 
            }
            //extract the chapter as HTML in a string
            string code = www.downloadHandler.text.Substring(ix + toBeSearched.Length);

            string chaptCode = String.Format("{0}.{1}", book, chpt);// the code of the chapter we a searching
            List<string> versesAdded = new(); // lsit of all the verses
            int max = ver.y; // how many verses to get (always one less than the number you put NOTE: i should change this)


            for (int i = ver.x; i < max; i++) // loop to check for all the verses we want
            {
                var line = code;
                //Debug.Log(line);
                //The string that indicates where paragraph that contains the verse (and other verse as well for some reason) start from
                string verseSpan = String.Format("<span data-usfm=\"{0}.{1}\" class=\"ChapterContent_verse__57FIw\">", chaptCode, i);
                //The string that indicates where the verse we are looking for starts from inside the paragraph of verses
                string verseSpanLable = String.Format("<span class=\"ChapterContent_label__R2PLt\">{0}</span>", i);//verse identifyer
                //The string that indicates where the next verse starts from                                                                                                    //
                string nextVerseSpanLable = String.Format("<span class=\"ChapterContent_label__R2PLt\">{0}</span>", i + 1);//verse identifyer 

                string CommentKey = "<span class=\"ChapterContent_note__YlDW0\">";//use to finde comments to remove 
                string HeadingKey = "<span class=\"ChapterContent_heading__xBDcs\">";//use to finde headings to remove 
                //The string that indicates where the actual text for a verse begins
                string TextKey = "<span class=\"ChapterContent_content__RrUqA\">";//Class that holds text in a verse 

                if (line.IndexOf(verseSpan) == -1) // if the paragraph couldnt be found
                {
                    //Debug.Log(verseSpan);
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
                        string verseNoHeadings = RemoveHeadings(filtertVerse, HeadingKey);
                        string verseNoComments = RemoveComments(verseNoHeadings, CommentKey, TextKey);//html verse without comments
                        versesAdded.Add(HTMLToText(verseNoComments));
                    }
                    else
                    {
                        // if there is no next verse then exstract the text to the end (This should be fine cuz the buttom of page stuff was removed)
                        string filtertVerse = txt.Substring(CurrentVerseindex); // raw html verse without unessary tags
                        string verseNoHeadings = RemoveHeadings(filtertVerse, HeadingKey);
                        string verseNoComments = RemoveComments(verseNoHeadings, CommentKey, TextKey);//html verse without comments
                        versesAdded.Add(HTMLToText(verseNoComments));
                    }

                }
            }
            //print text to screen
            m_Text.text = RemoveDuplicates(versesAdded);  
        }
    }

    /// <summary>
    /// Remove Heading Text from HTML string
    /// </summary>
    /// <param name="filterdVerse">Raw HTML verse without other verses</param>
    /// <param name="HeadingKey">The HTML span that contains the Headings</param>
    /// <returns>A HTML string without Headings</returns>
    string RemoveHeadings(string filterdVerse, string HeadingKey)
    {
        string EndKey = "</span>";

        var startIndexes = filterdVerse.AllIndexesOf(HeadingKey);// find all heading start indexes
        var builder = new StringBuilder(filterdVerse);
        for (int i = startIndexes.Count - 1; i >= 0; i--)
        {
            int startInd = startIndexes[i];
            int endInd = filterdVerse.IndexOf(EndKey, startInd);

            if (startInd == -1 || endInd == -1) // if therse no comments 
            {
               continue; //return the verse
            }

            int dist = endInd - startInd; // how many characters to remove (Cant be 0)
            if (dist < 0) { dist = 0; }

            builder.Remove(startInd, dist);
        }

        return builder.ToString();
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

        if (commentStartIndex == -1) // if therse no comments 
        {
            return filterdVerse; //return the verse
        }

        int commentEndIndex = filterdVerse.IndexOf(textKey, commentStartIndex)/*FindIndexAfter(textKey, commentKey, filterdVerse)*/;
        
        if(commentEndIndex == -1) // if therse no comments 
        {
            return filterdVerse; //return the verse
        }
        int count = commentEndIndex - commentStartIndex; // how many characters to remove (Cant be 0)
        if (count < 0) { count = 0; }
        return filterdVerse.Remove(commentStartIndex, count);
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
            _ = StartCoroutine(GetText(BookFinder.Find(Book), Chapter, verses));
        }
    }
}
