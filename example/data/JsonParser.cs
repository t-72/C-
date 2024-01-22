using Interfaces;
using System;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace Data
{
    public class JsonParser : ITreeParseable
    {
        private enum Stage_
        {
            Unknown,
            NewObject,
            EndObject
        }

        public static Int64 DataSize(string fileName)
        {
            Int64 result = 0;
            string text = GetText_(fileName);

            if (!string.IsNullOrEmpty(text))
            {
                string pattern = @"""size""\s+:\s+(\d+)";
                Match m = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (m.Success)
                    result = Int64.Parse(m.Groups[1].Value);
            }

            return result;
        }

        public JsonParser(string fileName)
        {
            fileName_ = fileName;
        }

        public void TreeParse(IAddSheetable addSheetable)
        {
            string text = GetText_(fileName_);

            if (!string.IsNullOrEmpty(text))
            {
                JsonTextReader reader = new JsonTextReader(new StringReader(text));

                if (IsNextObject_(reader))
                {
                    Stack<Int64> stackId = new Stack<Int64>();
                    stackId.Push(0);
                    TreeParse_(stackId, reader, addSheetable);
                }
            }
        }

        private static string GetText_(string fileName)
        {
            using (StreamReader sr = new StreamReader(fileName))
                return sr.ReadToEnd();
        }

        private void TreeParse_(Stack<Int64> stackId, JsonTextReader reader, IAddSheetable addSheetable)
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            FindName_(reader, addSheetable.Names(), map);

            Int64 idParent = stackId.Peek();
            Int64 idSheet = addSheetable.AddSheet(idParent, map);
            stackId.Push(idSheet);

            if (IsNextObject_(reader, stackId))
                TreeParse_(stackId, reader, addSheetable);
        }

        private void FindName_(JsonTextReader reader, IList<string> names, IDictionary<string, string> map)
        {
            string token = string.Empty;
            string name = string.Empty;
            List<string> names_ = new List<string>();

            while (reader.Read() && (reader.Value != null))
            {
                string tt = reader.TokenType.ToString();
                if (tt == "PropertyName")
                {
                    name = reader.Value.ToString();
                    if (names.Contains(name) && !names_.Contains(name))
                    {
                        if (!reader.Read() || (reader.Value == null))
                            break;
                        map.Add(name, reader.Value.ToString());
                        names_.Add(name);
                    }
                }
            }
        }

        private bool IsNextObject_(JsonTextReader reader, Stack<Int64> stackId = null)
        {
            bool result = false;
            bool isNext = true;

            while (isNext)
            {
                string tt = reader.TokenType.ToString();
                if (tt == "StartObject")
                {
                    result = true;
                    break;
                }
                else if (tt == "EndObject")
                {
                    if (stackId != null)
                        stackId.Pop();
                }
                isNext = reader.Read();
            }
            return result;
        }

        private string fileName_;
    }

}
