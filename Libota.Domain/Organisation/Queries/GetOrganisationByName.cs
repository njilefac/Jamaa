namespace Domain.Organisation.Queries
{
    public record GetOrganisationByName(string Name)
    {
        public string Name { get; } = Name;
    }
}