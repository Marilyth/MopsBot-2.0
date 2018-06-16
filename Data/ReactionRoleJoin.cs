using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace MopsBot.Data
{
    public class ReactionRoleJoin
    {
        //Key: Channel ID, Value: Message IDs
        public Dictionary<ulong, HashSet<ulong>> RoleInvites = new Dictionary<ulong, HashSet<ulong>>();

        public ReactionRoleJoin()
        {
            using (StreamReader read = new StreamReader(new FileStream($"mopsdata//ReactionRoleJoin.json", FileMode.OpenOrCreate)))
            {
                try
                {
                    RoleInvites = JsonConvert.DeserializeObject<Dictionary<ulong, HashSet<ulong>>>(read.ReadToEnd());
                    if (RoleInvites == null){
                        RoleInvites = new Dictionary<ulong, HashSet<ulong>>();
                    }
                    foreach (var channel in RoleInvites)
                    {
                        foreach (var message in channel.Value)
                        {
                            var textmessage = (IUserMessage)((ITextChannel)Program.client.GetChannel(channel.Key)).GetMessageAsync(message).Result;
                            Program.reactionHandler.addHandler(textmessage, new Emoji("✅"), JoinRole).Wait();
                            Program.reactionHandler.addHandler(textmessage, new Emoji("❎"), LeaveRole).Wait();
                            Program.reactionHandler.addHandler(textmessage, new Emoji("🗑"), DeleteInvite).Wait();
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message + e.StackTrace);
                }
            }
        }

        public void SaveJson()
        {
            using (StreamWriter write = new StreamWriter(new FileStream($"mopsdata//ReactionRoleJoin.json", FileMode.Create)))
                write.Write(JsonConvert.SerializeObject(RoleInvites, Formatting.Indented));
        }

        public async Task AddInvite(ITextChannel channel, string name)
        {
            SocketRole role = (SocketRole)channel.Guild.Roles.First(x => x.Name.ToLower().Equals(name.ToLower()));
            EmbedBuilder e = new EmbedBuilder();
            e.Title = role.Name + " Role Invite";
            e.Description = $"To join/leave the " + (role.IsMentionable ? role.Mention : role.Name) + " role, press the ✅/❎ Icons below this message!\n" +
                            "If you can manage Roles, you may delete this invitation by pressing the 🗑 Icon.";
            e.Color = role.Color;

            var author = new EmbedAuthorBuilder();
            e.AddField("Members in role", role.Members.Count(), true);

            var message = await channel.SendMessageAsync("", embed: e.Build());
            await Program.reactionHandler.addHandler(message, new Emoji("✅"), JoinRole);
            await Program.reactionHandler.addHandler(message, new Emoji("❎"), LeaveRole);
            await Program.reactionHandler.addHandler(message, new Emoji("🗑"), DeleteInvite);

            if (RoleInvites.ContainsKey(channel.Id)) RoleInvites[channel.Id].Add(message.Id);
            else {
                RoleInvites.Add(channel.Id, new HashSet<ulong>());
                RoleInvites[channel.Id].Add(message.Id);
            }

            SaveJson();
        }

        private async Task JoinRole(ReactionHandlerContext context)
        {
            var roleName = context.message.Embeds.First().Title.Split(" Role Invite")[0];
            var role = ((ITextChannel)context.channel).Guild.Roles.First(x => x.Name.Equals(roleName));
            var user = await ((ITextChannel)context.channel).Guild.GetUserAsync(context.reaction.UserId);
            await user.AddRoleAsync(role);            
            await updateMessage(context, (SocketRole) role);
        }

        private async Task LeaveRole(ReactionHandlerContext context)
        {
            var roleName = context.message.Embeds.First().Title.Split(" Role Invite")[0];
            var role = ((ITextChannel)context.channel).Guild.Roles.First(x => x.Name.Equals(roleName));
            var user = await ((ITextChannel)context.channel).Guild.GetUserAsync(context.reaction.UserId);
            await user.RemoveRoleAsync(role);                
            await updateMessage(context, (SocketRole) role);
        }

        private async Task DeleteInvite(ReactionHandlerContext context)
        {
            var user = await ((ITextChannel)context.channel).Guild.GetUserAsync(context.reaction.UserId);
            if (user.GuildPermissions.ManageRoles)
            {
                await Program.reactionHandler.clearHandler(context.message);

                if(RoleInvites[context.channel.Id].Count > 1)
                    RoleInvites[context.channel.Id].Remove(context.message.Id);
                else
                    RoleInvites.Remove(context.channel.Id);
                
                SaveJson();
            }
        }

        private async Task updateMessage(ReactionHandlerContext context, SocketRole role)
        {
            var e = context.message.Embeds.First().ToEmbedBuilder();

            e.Color = role.Color;

            foreach (EmbedFieldBuilder field in e.Fields)
            {
                if (field.Name.Equals("Members in role"))
                    field.Value = role.Members.Count();
            }

            await context.message.ModifyAsync(x =>
            {
                x.Embed = e.Build();
            });
        }
    }
}