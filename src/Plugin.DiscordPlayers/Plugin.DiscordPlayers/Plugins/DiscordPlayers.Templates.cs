using System.Collections.Generic;
using DiscordPlayersPlugin.Cache;
using DiscordPlayersPlugin.Configuration;
using DiscordPlayersPlugin.Templates;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Entities.Interactions.MessageComponents;
using Oxide.Ext.Discord.Entities.Permissions;
using Oxide.Ext.Discord.Extensions;
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
                TemplateNameCache cache = command.NameCache;
                
                DiscordMessageTemplate template = CreateBaseMessage();
                _templates.RegisterLocalizedTemplateAsync(this, cache.TemplateName, template, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
                
                DiscordEmbedFieldTemplate field = command.Command == "playersadmin" ? GetDefaultAdminFieldTemplate() : GetDefaultFieldTemplate();
                _field.RegisterLocalizedTemplateAsync(this, cache.TemplateName, field, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));

                if (command.EmbedsPerMessage == 1)
                {
                    DiscordEmbedTemplate embed = GetDefaultEmbedTemplate();
                    _embed.RegisterLocalizedTemplateAsync(this, cache.GetFirstEmbedName(), embed, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
                }
                else if (command.EmbedsPerMessage >= 2)
                {
                    DiscordEmbedTemplate first = GetFirstEmbedTemplate();
                    _embed.RegisterLocalizedTemplateAsync(this, cache.GetFirstEmbedName(), first, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
                    
                    DiscordEmbedTemplate last = GetLastEmbedTemplate();
                    _embed.RegisterLocalizedTemplateAsync(this, cache.GetLastEmbedName(), last, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
                }

                if (command.EmbedsPerMessage >= 3)
                {
                    DiscordEmbedTemplate middle = GetMiddleEmbedTemplate();
                    _embed.RegisterLocalizedTemplateAsync(this, cache.GetMiddleEmbedName(), middle, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
                }
            }

            DiscordMessageTemplate unknownState = CreateTemplateEmbed("Error: Failed to find a state for this message. Please create a new message.", DiscordColor.Danger.ToHex());
            _templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.Errors.UnknownState, unknownState, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
        }

        public DiscordMessageTemplate CreateBaseMessage()
        {
            return new DiscordMessageTemplate
            {
                Content = string.Empty,
                Components =
                {
                    new ButtonTemplate("Back", ButtonStyle.Primary, $"{BackCommand} {{discordplayers.command.id}}", "⬅"),
                    new ButtonTemplate("Page: {discordplayers.state.page}/{discordplayers.page.max}", ButtonStyle.Primary, "PAGE"),
                    new ButtonTemplate("Next", ButtonStyle.Primary, $"{ForwardCommand} {{discordplayers.command.id}}", "➡"),
                    new ButtonTemplate("Refresh", ButtonStyle.Primary, $"{RefreshCommand} {{discordplayers.command.id}}", "🔄"),
                    new ButtonTemplate("Sorted By: {discordplayers.state.sort}", ButtonStyle.Primary, $"{ChangeSort} {{discordplayers.command.id}}")
                }
            };
        }

        public DiscordEmbedTemplate GetDefaultEmbedTemplate()
        {
            return new DiscordEmbedTemplate
            {
                Title = "{server.name}",
                Description = "{server.players}/{server.players.max} Online Players | {server.players.loading} Loading | {server.players.queued} Queued",
                Color = DiscordColor.Blurple.ToHex(),
                TimeStamp = true,
                Footer =
                {
                    Enabled = true,
                    Text = "{plugin.title} V{plugin.version} by {plugin.author}",
                    IconUrl = PluginIcon
                }
            };
        }

        public DiscordEmbedTemplate GetFirstEmbedTemplate()
        {
            return new DiscordEmbedTemplate
            {
                Title = "{server.name}",
                Description = "{server.players}/{server.players.max} Online Players | {server.players.loading} Loading | {server.players.queued} Queued",
                Color = DiscordColor.Blurple.ToHex()
            };
        }

        public DiscordEmbedTemplate GetMiddleEmbedTemplate()
        {
            return new DiscordEmbedTemplate
            {
                Color = DiscordColor.Blurple.ToHex()
            };
        }
        
        public DiscordEmbedTemplate GetLastEmbedTemplate()
        {
            return new DiscordEmbedTemplate
            {
                Color = DiscordColor.Blurple.ToHex(),
                TimeStamp = true,
                Footer =
                {
                    Enabled = true,
                    Text = "{plugin.title} V{plugin.version} by {plugin.author}",
                    IconUrl = PluginIcon
                }
            };
        }

        public DiscordEmbedFieldTemplate GetDefaultFieldTemplate()
        {
            return new DiscordEmbedFieldTemplate("{discordplayers.player.index} {discordplayers.player.clantag}{player.name}", "**Online For:** {discordplayers.duration.hours}h {discordplayers.duration.minutes}m {discordplayers.duration.seconds}s");
        }
        
        public DiscordEmbedFieldTemplate GetDefaultAdminFieldTemplate()
        {
            return new DiscordEmbedFieldTemplate("{discordplayers.player.index} {discordplayers.player.clantag}{player.name}", "**Steam ID:**{player.id}\n**Online For:** {discordplayers.duration.hours}h {discordplayers.duration.minutes}m {discordplayers.duration.seconds}s\n**Ping:** {player.ping}ms\n**Country:** {player.address.data!country}");
        }
        
        public DiscordMessageTemplate CreateTemplateEmbed(string description, string color)
        {
            return new DiscordMessageTemplate
            {
                Embeds = new List<DiscordEmbedTemplate>
                {
                    new DiscordEmbedTemplate
                    {
                        Description = $"[{{plugin.title}}] {description}",
                        Color = color
                    }
                }
            };
        }
    }
}