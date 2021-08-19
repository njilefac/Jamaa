using AutoMapper;
using Club.Station.Data.Models;
using Domain.Entities.Shared;

namespace Club.Station.Data.Mapping
{
    public class EntityMappingProfile: Profile
    {
        public EntityMappingProfile()
        {
            CreateMap<OrganizationDto, Organization>()
                .ForMember(d => d.Fees, opt => opt.Ignore())
                .ForMember(d => d.Members, opt => opt.Ignore())
                .ReverseMap();
        }
    }
}