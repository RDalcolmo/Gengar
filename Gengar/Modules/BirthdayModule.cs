﻿using Discord;
using Discord.Interactions;
using Gengar.Models;
using Gengar.Options;
using Gengar.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Text;

namespace Gengar.Modules;

[Group("bday", "Birthday commands")]
public class BirthdayModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IOptions<DiscordOptions> _options;
    private readonly BirthdayService _birthdayService;

    public BirthdayModule(IOptions<DiscordOptions> options, BirthdayService birthdayService)
    {
        _options = options;
        _birthdayService = birthdayService;
    }

    [SlashCommand("next", "Checks if there are any birthdays within 14 days")]
    public async Task CheckBirthdays()
    {
        var users = (await _birthdayService.GetAllUsers()).Where(b => b.Birthday.DayOfYear >= DateTime.Now.DayOfYear && b.Birthday.DayOfYear <= DateTime.Now.AddDays(14).DayOfYear).ToList();

        var numberOfBirthdays = users.Count;
        string _content;

        if (numberOfBirthdays == 0)
        {
            _content = "There are no birthdays in the next 14 days!";
        }
        else
        {
            _content = $"There {(numberOfBirthdays > 1 ? $"are {numberOfBirthdays} upcoming birthdays" : "is 1 upcoming birthday")}!"
                        + $"\nThe next person's birthday is:";

            foreach (var person in users.OrderBy(m => m.Birthday.Month).ThenBy(d => d.Birthday.Day))
            {
                _content += $"\n<@{person._id}> on {person.Birthday:MMMM dd}!";
            }
        }

        await RespondAsync(_content, ephemeral: true);
    }

    [SlashCommand("month", "Gets a list of all birthdays in the chosen month")]
    public async Task BirthdayInMonth([Summary(description: "Choose the month")] Month month)
    {
        var users = await _birthdayService.GetAllUsers();

        users = users.Where(x => x.Birthday.Month == (int)month).ToList();

        var numberOfBirthdays = users.Count;
        StringBuilder _content = new();

        if (numberOfBirthdays == 0)
        {
            _content.Append("There are no birthdays in this month!");
        }
        else
        {
            _content.AppendLine($"There {(numberOfBirthdays > 1 ? $"are {numberOfBirthdays} birthdays" : "is 1 birthday")} in the month of {month}!")
                    .AppendLine($"Birthdays found in this month are:");

            foreach (var person in users.OrderBy(m => m.Birthday.Month).ThenBy(d => d.Birthday.Day))
            {
                _content.AppendLine($"<@{person._id}> on {person.Birthday:MMMM dd}!");
            }
        }

        await RespondAsync(_content.ToString(), ephemeral: true);
    }

    [SlashCommand("when", "Gets the birthday date of the given discord user ID")]
    public async Task WhenIsBirthday([Summary(description: "Discord user ID")] IUser userid)
    {
        var person = await _birthdayService.GetUserById(userid.Id);
        string _content;

        if (person == null)
        {
            _content = "This person does not have a birthday registered in our database!";
        }
        else
        {
            _content = $"<@{person._id}>'s Birthday is on {person.Birthday:MMMM dd}";
        }

        await RespondAsync(_content, ephemeral: true);
    }

    [SlashCommand("remove", "Removes a birthday from the database")]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    [RequireOwner]
    public async Task RemoveUser([Summary(description: "Discord user ID")] IUser userid)
    {
        await _birthdayService.Remove(userid.Id);

        var _content = $"Removed user <@{userid.Id}>'s Birthday.";

        await RespondAsync(_content, ephemeral: true);
    }

    [SlashCommand("add", "Adds a birthday to the database")]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    [RequireOwner]
    public async Task AddUser([Summary(description: "Discord user ID")] IUser userid,
                              [Summary(description: "Birthday month")] Month month,
                              [Summary(description: "Birthday day of the month")] int day)
    {
        await _birthdayService.Patch(new Models.Mongo.Birthdays()
        {
            _id = userid.Id,
            Birthday = new DateTime(2019, (int)month, day)
        });

        var _content = $"Added {userid.Mention} to the database on {month} {day}.";

        await RespondAsync(_content, ephemeral: true);
    }

    [SlashCommand("bcast", "Broadcasts the birthday messages to the channel set")]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    [RequireOwner]
    [RequireContext(ContextType.Guild)]
    public async Task Broadcast()
    {
        await Console.Out.WriteLineAsync($"Broadcasting today's birthdays: {DateTime.Today.ToLongDateString()}");

        var Guild = Context.Guild;

        if (Guild == null)
        {
            return;
        }

        await Console.Out.WriteLineAsync($"Detected Guild: {Guild.Name}");
        var Channel = Guild.GetTextChannel(_options.Value.ChannelId);

        if (Channel == null)
        {
            return;
        }

        await Console.Out.WriteLineAsync($"Detected Broadcast Channel: {Channel.Name}");

        var birthday = await _birthdayService.GetAllUsers();
        birthday = birthday.Where(x => x.Birthday.Month == DateTime.Today.Month && x.Birthday.Day == DateTime.Today.Day).ToList();

        var numberOfBirthdays = birthday.Count;

        if (numberOfBirthdays == 0)
        {
            await RespondAsync("There are no birthdays today", ephemeral: true);
            return;
        }

        await Console.Out.WriteLineAsync($"Total birthdays today: {birthday.Count}");

        foreach (var user in birthday.ToList())
        {
            var userInGuild = Guild.GetUser(user._id);

            if (userInGuild == null)
            {
                birthday.Remove(user);
            }
        }

        StringBuilder _content = new($"There {(numberOfBirthdays > 1 ? $"are {numberOfBirthdays} birthdays" : "is 1 birthday")} today!\n");
        foreach (var person in birthday)
        {
            _content.AppendLine($"It's <@{person._id}> birthday today!! Happy birthday!");
        }

        await Channel.SendMessageAsync(_content.ToString());
    }
}

[Group("bcast", "Group owner commands")]
[DefaultMemberPermissions(GuildPermission.ManageGuild)]
[RequireContext(ContextType.Guild)]
public class RegistrationModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IOptions<DiscordOptions> _options;

    public RegistrationModule(IOptions<DiscordOptions> options)
    {
        _options = options;
    }

    [SlashCommand("set", "Sets a new channel to broadcast birthdays in")]
    public async Task SetToChannel()
    {
        _options.Value.ChannelId = Context.Channel.Id;
        string _content = "Broadcasting channel has been changed. Birthday messages will now be posted here.";

        await RespondAsync(_content, ephemeral: true);
    }

    [SlashCommand("remove", "Stops broadcasting birthdays to the server")]
    public async Task RemoveFromChannel()
    {
        _options.Value.ChannelId = 0;
        await RespondAsync("The current broadcasting channel has been removed.", ephemeral: true);
    }
}
