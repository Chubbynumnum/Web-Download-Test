using System;
using System.Collections;
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
            string toBeSearched = "ChapterContent_p__dVKHb";
            int ix = www.downloadHandler.text.IndexOf(toBeSearched);

            if (ix != -1)
            {
                string code = www.downloadHandler.text.Substring(ix + toBeSearched.Length);
                string[] lines = code.Split(new[] { "ChapterContent_p__dVKHb" }, StringSplitOptions.None);
                lines[^1] = lines[^1].Split(new[] { "</div>" }, StringSplitOptions.None)[0];


                // do something here
                Debug.Log("GetText");
                foreach (string line in lines)
                {
                    //// Show results as text
                    //Debug.Log(line);
                }

                string chaptCode = "JHN.1";
                string verse = "";
                int numberOfchecks = 0;
                foreach (string line in lines)
                {
                    numberOfchecks = 0;

                    for (int i = 0; i < 6; i++) // next 5 verses
                    {
                        if(line.IndexOf(chaptCode + "." + i.ToString()) == -1)
                        {
                            numberOfchecks++;
                            continue; //next chapter doesnt exsist
                        }

                        if (i != 0) { verse += " "; } // add space inbetween each verse
                        verse += HTMLToText(line);
                    }

                    if (numberOfchecks == 5) { break; } //stop check paragraphs if there is no more matches
                }

                m_Text.text = verse;
                Debug.Log(verse);
            }

            //// Or retrieve results as binary data
            //byte[] results = www.downloadHandler.data;
        }
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
        HTMLCode = Regex.Replace(HTMLCode, "=\"", "");
        HTMLCode = Regex.Replace(HTMLCode, "<div class", "");
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
