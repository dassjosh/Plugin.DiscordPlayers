using System.Collections.Generic;
using DiscordPlayersPlugin.Configuration;

namespace DiscordPlayersPlugin.Cache
{
    public class TemplateNameCache
    {
        public readonly string TemplateName;
        private readonly List<string> _embedNames;

        public TemplateNameCache(BaseMessageSettings settings)
        {
            _embedNames = new List<string>(settings.EmbedsPerMessage);
            string name = settings.GetTemplateName();
            TemplateName = char.ToUpper(name[0]) + name.Substring(1);
            SetEmbedNames(settings.EmbedsPerMessage);
        }

        private void SetEmbedNames(int embedLimit)
        {
            if (embedLimit == 1)
            {
                _embedNames.Add(TemplateName);
                return;
            }
            
            _embedNames.Add($"{TemplateName}.{{First}}");

            string middle = $"{TemplateName}.{{Middle}}";
            for (int i = 1; i < embedLimit - 1; i++)
            {
                _embedNames.Add(middle);
            }
            
            _embedNames.Add($"{TemplateName}.{{Last}}");
        }

        public string GetEmbedName(int index) => _embedNames[index];
        public string GetFirstEmbedName() => GetEmbedName(0);
        public string GetLastEmbedName() => GetEmbedName(_embedNames.Count - 1);
        public string GetMiddleEmbedName() => _embedNames[1];
    }
}