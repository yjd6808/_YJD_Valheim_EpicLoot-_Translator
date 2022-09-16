// See https://aka.ms/new-console-template for more information
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;
using JsonTranslator;
using MoreLinq;
using MoreLinq.Extensions;
using MoreLinq.Experimental;
using System.Web;

static bool CheckYes(string t)
{
    return t == "y" || t == "Y" || t.StartsWith("ㅛ");
}

static bool TryPapagoTranslate(string english, out string korean)
{
    string url = "https://openapi.naver.com/v1/papago/n2mt";
    string text = String.Empty;
    JObject trasnlateResult = null;
    try
    {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Headers.Add("X-Naver-Client-Id", "0pDIhlTmK4viGCw3BCjo");
        request.Headers.Add("X-Naver-Client-Secret", "PJIWEK35Yc");
        request.Method = "POST";
        byte[] byteDataParams = Encoding.UTF8.GetBytes("source=en&target=ko&text=" + english);
        request.ContentType = "application/x-www-form-urlencoded";
        request.ContentLength = byteDataParams.Length;
        Stream st = request.GetRequestStream();
        st.Write(byteDataParams, 0, byteDataParams.Length);
        st.Close();
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        Stream stream = response.GetResponseStream();
        StreamReader reader = new StreamReader(stream, Encoding.UTF8);
        text = reader.ReadToEnd();
        stream.Close();
        response.Close();
        reader.Close();
        trasnlateResult = JObject.Parse(text);
    } 
    catch {}

    
    string trasnlatedText = string.Empty;
    try 
    {
        if (trasnlateResult != null)
            trasnlatedText = trasnlateResult["message"]["result"]["translatedText"].ToString(); 
    } 
    catch 
    { 
    }
    korean = trasnlatedText;

    if (trasnlatedText == string.Empty)
        return false;

    return true;
}


static bool TryGoogleTranslate(string english, out string korean)
{
    try
    {
        
        var fromLanguage = "en";
        var toLanguage = "ko";

        var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLanguage}&tl={toLanguage}&dt=t&q={HttpUtility.UrlEncode(english)}";
        var webClient = new WebClient
        {
            Encoding = System.Text.Encoding.UTF8
        };
        var result = webClient.DownloadString(url);
        if (result.Contains("windows-1251"))
        {
            webClient.Encoding = System.Text.Encoding.GetEncoding("windows-1251");
            result = webClient.DownloadString(url);
        }
        else if (result.Contains("ISO-8859-2"))
        {
            webClient.Encoding = System.Text.Encoding.GetEncoding("ISO-8859-2");
            result = webClient.DownloadString(url);
        }
        korean = result.Substring(4, result.IndexOf("\"", 4, StringComparison.Ordinal) - 4);
        return true;
    }
    catch (Exception exc)
    {
        korean = String.Empty;
        return false;
    }
}

Console.Write("경로 입력 >");
string? path = Console.ReadLine();

if (!File.Exists(path))
{
    Console.WriteLine("해당 경로의 파일을 찾을 수 없습니다.");
    return;
}

JObject traslationTarget = null;
try { traslationTarget = JObject.Parse(File.ReadAllText(path)); } catch { }

if (traslationTarget == null)
{
    Console.WriteLine("JObject 형식으로 변환하는데 실패했습니다.");
    return;
}

var enumerator = traslationTarget.GetEnumerator().ToList();
int pos = 0;

if (enumerator.Count == 0)
{
    Console.WriteLine("수정 가능한 데이터가 없습니다.");
    return;
}

while (true)
{
    KeyValuePair<string, JToken?> current = enumerator[pos];

    string key = current.Key;
    string value = "문자열이 아님";

    if (current.Value != null && current.Value.Type == JTokenType.String)
        value = current.Value.ToString();

    Console.WriteLine($"키 {key}");
    Console.WriteLine($"값 {value}");

    Console.WriteLine($"[메뉴] {pos}/{enumerator.Count - 1}");
    Console.WriteLine("1. 다음위치로 이동");
    Console.WriteLine("2. 이전위치로 이동");
    Console.WriteLine("3. 특정인덱스 위치로 이동");
    Console.WriteLine("4. 특정키 위치로 이동");
    Console.WriteLine("5. 현재위치 파파고 번역");
    Console.WriteLine("6. 현재위치 구글 번역");
    Console.WriteLine("7. 현재위치 수동 번역");
    Console.WriteLine("8. Json으로 저장");
    Console.WriteLine("9. 텍스트로 저장");
    Console.WriteLine("10. 특정 문자열 전체 치환");

    Console.Write("선택 >");
    int.TryParse(Console.ReadLine(), out int choose);

    switch (choose)
    {
        case 1:
        {
            pos = pos >= enumerator.Count ? 0 : pos + 1;
            break;
        }
        case 2:
        {
            pos = pos == 0 ? enumerator.Count - 1 : pos - 1;
            break;
        }
        case 3:
        {
             Console.Write("이동할 위치 >");
             if (int.TryParse(Console.ReadLine(), out int nextpos) && nextpos >= 0 && nextpos < enumerator.Count)
             {
                 pos = nextpos;
                 continue;
             }

             Console.WriteLine("해당 위치로는 이동할 수 없습니다.");
             break;
        }
        case 4:
        {
            Console.Write("이동할 키 >");
            string next_key = Console.ReadLine();
            int next_pos = enumerator.FindIndex(x => x.Key == next_key);
            if (next_key.Length > 0 && next_pos >= 0)
            {
                pos = next_pos;
                continue;
            }

            Console.WriteLine("해당 위치로는 이동할 수 없습니다.");
            break;
        }
        case 5:
        {
            if (TryPapagoTranslate(value, out string translated))
            {
                Console.WriteLine($"파파고 번역 결과 : {translated}");
                Console.WriteLine("변역한 결과로 변경 하시겠습니까? (y/Y) >");
                string question = Console.ReadLine();
                if (CheckYes(question))
                {
                    traslationTarget[key] = translated;
                    enumerator = traslationTarget.GetEnumerator().ToList();
                    File.WriteAllText(path, traslationTarget.ToString());
                }
                continue;
            }

            Console.WriteLine("값을 변역하는데 실패했습니다.");
            break;
        }
        case 6:
        {
            if (TryGoogleTranslate(value, out string translated))
            {
                Console.WriteLine($"구글 번역 결과 : {translated}");
                Console.WriteLine("변역한 결과로 변경 하시겠습니까? (y/Y) >");
                string question = Console.ReadLine();
                if (CheckYes(question))
                {
                    traslationTarget[key] = translated;
                    enumerator = traslationTarget.GetEnumerator().ToList();
                    File.WriteAllText(path, traslationTarget.ToString());
                }
                continue;
            }

            Console.WriteLine("값을 변역하는데 실패했습니다.");
            break;
        }
        case 7:
        {
            Console.Write("변역할 내용 : ");
            string translated = Console.ReadLine();
            Console.WriteLine("변역한 결과로 변경 하시겠습니까? (y/Y) >");
            string question = Console.ReadLine();
            if (CheckYes(question))
            {
                traslationTarget[key] = translated;
                enumerator = traslationTarget.GetEnumerator().ToList();
                File.WriteAllText(path, traslationTarget.ToString());
            }
            break;
        }
        case 8:
        {
            Console.Write("저장할 파일 명 : ");
            string? filename = Console.ReadLine();

            if (filename.Length == 0)
                Console.WriteLine("파일 명을 똑바로 입력해주세요.");
            else
                File.WriteAllText(filename + ".json", traslationTarget.ToString());
            break;
        }
        case 9:
        {
            Console.Write("저장할 파일 명 : ");
            string? filename = Console.ReadLine();

            if (filename.Length == 0)
                Console.WriteLine("파일 명을 똑바로 입력해주세요.");
            else
            {
                StringBuilder builder = new StringBuilder(100000);
                for (int i = 0; i < enumerator.Count; i++)
                    builder.AppendLine($"{i} : {enumerator[i].Key} : {enumerator[i].Value}");
                File.WriteAllText(filename + ".txt", builder.ToString());
            }
            break;
        }
        case 10:
        {
            Console.Write("치환될 영어 문자열 : ");
            string? en = Console.ReadLine();
            Console.Write("치환할 한글 문자열 : ");
            string? ko = Console.ReadLine();
            Console.Write("대소문자 무시여부(y/Y)");
            string? ignorecase = Console.ReadLine();
            


            if (en == null || ko == null || ignorecase == null || en.Length == 0 || ko.Length == 0 || ignorecase.Length == 0)
                Console.WriteLine("똑바로 입력해주세요.");
            else
            {
                bool ignorecase_b = CheckYes(ignorecase);
                for (int i = 0; i < enumerator.Count; i++)
                {
                    traslationTarget[enumerator[i].Key] = ignorecase_b ?
                            enumerator[i].Value.ToString().Replace(en, ko, StringComparison.OrdinalIgnoreCase) :
                            enumerator[i].Value.ToString().Replace(en, ko, StringComparison.Ordinal);
                }

                enumerator = traslationTarget.GetEnumerator().ToList();
                File.WriteAllText(path, traslationTarget.ToString());
            }
            break;
        }
    }
}


