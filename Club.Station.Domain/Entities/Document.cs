namespace Domain.Entities
{
    using System;

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
}
