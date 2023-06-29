using System.Collections.Generic;
using DiscordPlayersPlugin.Cache;
using DiscordPlayersPlugin.Configuration;
using DiscordPlayersPlugin.Templates;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Entities.Interactions.MessageComponents;
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
                DiscordEmbedFieldTemplate embed = command.Command == "playersadmin" ? GetDefaultAdminFieldTemplate() : GetDefaultFieldTemplate();
                CreateCommandTemplates(command, embed, false);
            }

            foreach (PermanentMessageSettings permanent in _pluginConfig.Permanent)
            {
                CreateCommandTemplates(permanent, GetDefaultFieldTemplate(), true);
            }

            DiscordMessageTemplate unknownState = CreateTemplateEmbed("Error: Failed to find a state for this message. Please create a new message.", DiscordColor.Danger);
            _templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.Errors.UnknownState, unknownState, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
            
            DiscordMessageTemplate unknownCommand = CreateTemplateEmbed("Error: Command not found '{discordplayers.command.name}'. Please create a new message", DiscordColor.Danger);
            _templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.Errors.UnknownCommand, unknownCommand, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
        }

        private void CreateCommandTemplates(BaseMessageSettings command, DiscordEmbedFieldTemplate @default, bool isGlobal)
        {
            TemplateNameCache cache = command.NameCache;

            DiscordMessageTemplate template = CreateBaseMessage();
            RegisterTemplate(_templates, cache.TemplateName, template, isGlobal, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
            RegisterTemplate(_field, cache.TemplateName, @default, isGlobal, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));

            if (command.EmbedsPerMessage == 1)
            {
                DiscordEmbedTemplate embed = GetDefaultEmbedTemplate();
                RegisterTemplate(_embed, cache.GetFirstEmbedName(), embed, isGlobal, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
            }
            else if (command.EmbedsPerMessage >= 2)
            {
                DiscordEmbedTemplate first = GetFirstEmbedTemplate();
                RegisterTemplate(_embed, cache.GetFirstEmbedName(), first, isGlobal, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));

                DiscordEmbedTemplate last = GetLastEmbedTemplate();
                RegisterTemplate(_embed, cache.GetLastEmbedName(), last, isGlobal, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
            }

            if (command.EmbedsPerMessage >= 3)
            {
                DiscordEmbedTemplate middle = GetMiddleEmbedTemplate();
                RegisterTemplate(_embed, cache.GetMiddleEmbedName(), middle, isGlobal, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
            }
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
                    new ButtonTemplate("Back", ButtonStyle.Primary, $"{BackCommand} {{discordplayers.command.id}}", "⬅"),
                    new ButtonTemplate("Page: {discordplayers.state.page}/{discordplayers.page.max}", ButtonStyle.Primary, "PAGE", false),
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
            return new DiscordEmbedFieldTemplate("{discordplayers.player.index} [{player.clan.tag}] {player.name}", "**Online For:** {discordplayers.duration.hours}h {discordplayers.duration.minutes}m {discordplayers.duration.seconds}s");
        }
        
        public DiscordEmbedFieldTemplate GetDefaultAdminFieldTemplate()
        {
            return new DiscordEmbedFieldTemplate("{discordplayers.player.index} [{player.clan.tag}] {player.name}", "**Steam ID:**{player.id}\n**Online For:** {discordplayers.duration.hours}h {discordplayers.duration.minutes}m {discordplayers.duration.seconds}s\n**Ping:** {player.ping}ms\n**Country:** {player.address.data!country}");
        }
        
        public DiscordMessageTemplate CreateTemplateEmbed(string description, DiscordColor color)
        {
            return new DiscordMessageTemplate
            {
                Embeds = new List<DiscordEmbedTemplate>
                {
                    new DiscordEmbedTemplate
                    {
                        Description = $"[{{plugin.title}}] {description}",
                        Color = color.ToHex()
                    }
                }
            };
        }
    }
}