using System;

namespace Domain.Shared;

public class Document
{
    public Document(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public Guid Id { get; }
    public string Name { get; }
}