namespace Libota.Application.Organisation.Events
{
    public class OrganisationCreated
    {
        public string Name { get; }
        public string? Description { get; }

        public OrganisationCreated(string name, string? description)
        {
            Name = name;
            Description = description;
        }
    }
}