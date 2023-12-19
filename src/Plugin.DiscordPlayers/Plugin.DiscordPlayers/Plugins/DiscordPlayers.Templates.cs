using System.Collections.Generic;
using DiscordPlayersPlugin.Configuration;
using DiscordPlayersPlugin.Placeholders;
using DiscordPlayersPlugin.Templates;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Entities.Interactions.MessageComponents;
using Oxide.Ext.Discord.Extensions;
using Oxide.Ext.Discord.Libraries.Placeholders.Keys;
using Oxide.Ext.Discord.Libraries.Templates;
using Oxide.Ext.Discord.Libraries.Templates.Components;
using Oxide.Ext.Discord.Libraries.Templates.Embeds;
using Oxide.Ext.Discord.Libraries.Templates.Messages;

namespace DiscordPlayersPlugin.Plugins
{
    public partial class DiscordPlayers
    {
        public void RegisterTemplates()
        {
            foreach (CommandSettings command in _pluginConfig.CommandMessages)
            {
                DiscordEmbedFieldTemplate embed = command.Command == "playersadmin" ? GetDefaultAdminFieldTemplate() : GetDefaultFieldTemplate();
                CreateCommandTemplates(command, embed, false);
            }

            foreach (PermanentMessageSettings permanent in _pluginConfig.Permanent)
            {
                DiscordEmbedFieldTemplate embed = permanent.TemplateName == "PermanentAdmin" ? GetDefaultAdminFieldTemplate() : GetDefaultFieldTemplate();
                CreateCommandTemplates(permanent, embed, true);
            }

            DiscordMessageTemplate unknownState = CreateTemplateEmbed("Error: Failed to find a state for this message. Please create a new message.", DiscordColor.Danger);
            _templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.Errors.UnknownState, unknownState, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
            
            DiscordMessageTemplate unknownCommand = CreateTemplateEmbed($"Error: Command not found '{PlaceholderKeys.CommandName}'. Please create a new message", DiscordColor.Danger);
            _templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.Errors.UnknownCommand, unknownCommand, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
        }

        private void CreateCommandTemplates(BaseMessageSettings command, DiscordEmbedFieldTemplate @default, bool isGlobal)
        {
            DiscordMessageTemplate template = CreateBaseMessage();
            var name = command.GetTemplateName();
            RegisterTemplate(_templates, name, template, isGlobal, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
            RegisterTemplate(_field, name, @default, isGlobal, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));

            DiscordEmbedTemplate embed = GetDefaultEmbedTemplate();
            RegisterTemplate(_embed, name, embed, isGlobal, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
        }

        public void RegisterTemplate<TTemplate>(BaseMessageTemplateLibrary<TTemplate> library, string name, TTemplate template, bool isGlobal, TemplateVersion version, TemplateVersion minVersion) where TTemplate : class, new()
        {
            if (isGlobal)
            {
                library.RegisterGlobalTemplateAsync(this, name, template, version, minVersion);
            }
            else
            {
                library.RegisterLocalizedTemplateAsync(this, name, template, version, minVersion);
            }
        }

        public DiscordMessageTemplate CreateBaseMessage()
        {
            return new DiscordMessageTemplate
            {
                Content = string.Empty,
                Components =
                {
                    new ButtonTemplate("Back", ButtonStyle.Primary, $"{BackCommand} {PlaceholderKeys.CommandId}", "⬅"),
                    new ButtonTemplate($"Page: {PlaceholderKeys.Page}/{PlaceholderKeys.MaxPage}", ButtonStyle.Primary, "PAGE", false),
                    new ButtonTemplate("Next", ButtonStyle.Primary, $"{ForwardCommand} {PlaceholderKeys.CommandId}", "➡"),
                    new ButtonTemplate("Refresh", ButtonStyle.Primary, $"{RefreshCommand} {PlaceholderKeys.CommandId}", "🔄"),
                    new ButtonTemplate($"Sorted By: {PlaceholderKeys.SortState}", ButtonStyle.Primary, $"{ChangeSort} {PlaceholderKeys.CommandId}")
                }
            };
        }

        public DiscordEmbedTemplate GetDefaultEmbedTemplate()
        {
            return new DiscordEmbedTemplate
            {
                Title = $"{DefaultKeys.Server.Name}",
                Description = $"{DefaultKeys.Server.Players}/{DefaultKeys.Server.MaxPlayers} Online Players | {{server.players.loading}} Loading | {{server.players.queued}} Queued",
                Color = DiscordColor.Blurple.ToHex(),
                TimeStamp = true,
                Footer =
                {
                    Enabled = true,
                    Text = $"{DefaultKeys.Plugin.Name} V{DefaultKeys.Plugin.Version} by {DefaultKeys.Plugin.Author}",
                    IconUrl = PluginIcon
                }
            };
        }

        public DiscordEmbedFieldTemplate GetDefaultFieldTemplate()
        {
            return new DiscordEmbedFieldTemplate($"#{PlaceholderKeys.PlayerIndex} {DefaultKeys.Player.NameClan}", $"**Connected:** {DefaultKeys.Timespan.Hours} {DefaultKeys.Timespan.Minutes}m {DefaultKeys.Timespan.Seconds}s");
        }
        
        public DiscordEmbedFieldTemplate GetDefaultAdminFieldTemplate()
        {
            return new DiscordEmbedFieldTemplate($"#{PlaceholderKeys.PlayerIndex} {DefaultKeys.Player.NameClan}", 
                $"**Steam ID:**{DefaultKeys.Player.Id}\n" +
                $"**Connected:** {DefaultKeys.Timespan.Hours} {DefaultKeys.Timespan.Minutes}m {DefaultKeys.Timespan.Seconds}s\n" +
                $"**Ping:** {DefaultKeys.Player.Ping}ms\n" +
                $"**Country:** {DefaultKeys.Player.Country}\n" +
                $"**User:** {DefaultKeys.User.Mention}");
        }
        
        public DiscordMessageTemplate CreateTemplateEmbed(string description, DiscordColor color)
        {
            return new DiscordMessageTemplate
            {
                Embeds = new List<DiscordEmbedTemplate>
                {
                    new DiscordEmbedTemplate
                    {
                        Description = $"[{DefaultKeys.Plugin.Title}] {description}",
                        Color = color.ToHex()
                    }
                }
            };
        }
    }
}