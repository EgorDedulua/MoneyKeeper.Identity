using AutoMapper;
using MoneyKeeper.Identity.Application.Contracts.Auth;
using MoneyKeeper.Identity.Contracts;

namespace MoneyKeeper.Identity.Profiles
{
    public class AuthResponseProfile : Profile
    {
        public AuthResponseProfile()
        {
            CreateMap<AuthResult, AuthResponse>();
        }
    }
}
