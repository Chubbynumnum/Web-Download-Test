using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using TetraCreations.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class Test : MonoBehaviour
{
    [SerializeField] TMP_Text m_Text;

    async Task GetDocumentAsync()
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
            string toBeSearched = "<div class=\"ChapterContent_p__dVKHb\">";
            int ix = www.downloadHandler.text.IndexOf(toBeSearched);

            if (ix != -1)
            {
                string code = www.downloadHandler.text.Substring(ix + toBeSearched.Length);
                string[] lines = code.Split(new[] { toBeSearched }, StringSplitOptions.None);
                lines[^1] = lines[^1].Split(new[] { "</div>" }, StringSplitOptions.None)[0];


                // do something here
                Debug.Log("GetText");
                //foreach (string line in lines)
                //{
                //    // Show results as text
                //    Debug.Log(line);
                //}

                string chaptCode = "JHN.1";
                List<string> versesAdded = new();
                int max = 10;

                
                
                //int numberOfchecks = 0;

                for (int i = 0; i < max; i++) // next 4 verses
                {
                    foreach (string line in lines)
                    {

                        string verseSpan = String.Format("<span data-usfm=\"{0}.{1}\" class=\"ChapterContent_verse__57FIw\">", chaptCode, i);
                        string verseSpanLable = String.Format("<span class=\"ChapterContent_label__R2PLt\">{0}</span>", i);//verse identifyer 
                        string nextVerseSpanLable = String.Format("<span class=\"ChapterContent_label__R2PLt\">{0}</span>", i + 1);//verse identifyer 
                        if (line.IndexOf(verseSpan) == -1)
                        {
                            //numberOfchecks++;
                            Debug.Log(verseSpan);
                            continue; //next chapter doesnt exsist
                        }
                        else
                        {
                            int NextVerseindex = line.Substring(line.IndexOf(verseSpan)).IndexOf(nextVerseSpanLable);
                            int CurrentVerseindex = line.Substring(line.IndexOf(verseSpan)).IndexOf(verseSpanLable);

                            Debug.Log("Current verse index = " + CurrentVerseindex + " Next verse index = " + NextVerseindex);

                            if(CurrentVerseindex < 0) { continue; }

                            if (NextVerseindex > 0)
                            {
                                var txt = line.Substring(line.IndexOf(verseSpan));
                                versesAdded.Add(HTMLToText(txt.Substring(CurrentVerseindex, NextVerseindex - CurrentVerseindex)));
                            }
                            else
                            {
                                var txt = line.Substring(line.IndexOf(verseSpan));
                                versesAdded.Add(HTMLToText(txt.Substring(CurrentVerseindex)));
                            }

                        }
                    }
                }

                    //if (numberOfchecks == max) { break; } //stop check paragraphs if there is no more matches
                
                //Debug.Log(RemoveDuplicates(versesAdded));
                m_Text.text = RemoveDuplicates(versesAdded);
                //Debug.Log(verse);
            }

            //// Or retrieve results as binary data
            //byte[] results = www.downloadHandler.data;
        }
    }
    string RemoveDuplicates(List<string> versesAdded)
    {
        var output = new StringBuilder();

        for (int i = 0; i < versesAdded.Count; i++)
        {
            string line = versesAdded[i];
            Debug.Log(line);
            string nextLine = versesAdded[Math.Clamp(i + 1, 0, versesAdded.Count - 1)];
            //if(nextLine.IndexOf(line) == -1)
            //{
                output.AppendJoin(" ", line);
            //}
        }

        return output.ToString();
    }

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
        //Remove div
        //HTMLCode = Regex.Replace(HTMLCode, "\">", "");
        //HTMLCode = Regex.Replace(HTMLCode, "=\"", "");
        //HTMLCode = Regex.Replace(HTMLCode, "<div class", "");
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
