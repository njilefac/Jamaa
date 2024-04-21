using AutoMapper;
using Domain.Users;
using Libota.Data.Models;
using Libota.Data.Models.Users;

namespace Libota.Data.Mapping
{
    public class EntityMappingProfile: Profile
    {
        public EntityMappingProfile()
        {
            CreateMap<User, UserData>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Account.Id))
                .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.Account.Credentials.UserName))
                .ForMember(d => d.Password, opt => opt.MapFrom(s => s.Account.Credentials.Password))
                .ForMember(d => d.Email, opt => opt.MapFrom(s => s.Account.Email))
                .ForMember(d => d.FirstName, opt => opt.MapFrom(s => s.FirstName))
                .ForMember(d => d.MiddleName, opt => opt.MapFrom(s => s.MiddleName))
                .ForMember(d => d.LastName, opt => opt.MapFrom(s => s.LastName))
                .ForMember(d => d.Gender, opt => opt.MapFrom(s => s.Gender))
                .ForMember(d => d.IsActive, opt => opt.MapFrom(s => s.Account.IsActive))
                .ForMember(d => d.IsSuperUser, opt => opt.MapFrom(s => s.Account.IsSuperUser))
                .ReverseMap();
        }
    }
}