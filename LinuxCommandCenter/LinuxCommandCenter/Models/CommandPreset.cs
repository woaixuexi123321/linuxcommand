using System;

namespace LinuxCommandCenter.Models;

public class CommandPreset
{
    public string Id { get; }
    public string Name { get; }
    public string Category { get; }
    public string CommandTemplate { get; }
    public string Description { get; }
    public bool RequiresConfirmation { get; }
    public bool IsDangerous { get; }

    public CommandPreset(
        string name,
        string category,
        string commandTemplate,
        string description,
        bool requiresConfirmation = false,
        bool isDangerous = false)
    {
        Id = Guid.NewGuid().ToString("N");
        Name = name;
        Category = category;
        CommandTemplate = commandTemplate;
        Description = description;
        RequiresConfirmation = requiresConfirmation;
        IsDangerous = isDangerous;
    }

    public override string ToString() => Name;
}