namespace Libota.Application.Organisation.Commands
{
    public class CreateOrganisation(string name, string? description)
    {
        public string Name { get; } = name;
        public string? Description { get; } = description;
    }
}