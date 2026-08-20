using AutoMapper;
using MoneyKeeper.Identity.Application.Events;
using MoneyKeeper.Identity.Core.Entities;

namespace MoneyKeeper.Identity.Application.Profiles
{
    public class UserRegisteredEventProfile : Profile
    {
        public UserRegisteredEventProfile()
        {
            CreateMap<User, UserRegisteredEvent>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id));
        }
    }
}
