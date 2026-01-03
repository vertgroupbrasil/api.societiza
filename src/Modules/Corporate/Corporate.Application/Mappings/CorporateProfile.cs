using AutoMapper;
using Core.Domain.Entities;
using Corporate.Domain.DTOs;
using Corporate.Domain.Entities;
using Corporate.Shared.Response.Accountancy;

namespace Corporate.Application.Mappings;

public class CorporateProfile : Profile
{
    public CorporateProfile()
    {
        TenantMappings();
        AccountancyMappings();
    }

    private void TenantMappings()
    {

    }

    private void AccountancyMappings()
    {
        CreateMap<Accountancy, AccountancyDetailResponse>()
            .ForMember(dest => dest.Tenant, opt => opt.MapFrom(src => src.Tenant));
        CreateMap<Accountancy, CreateAccountancyResponse>();

        CreateMap<Accountancy, AccountancyListResponse>()
            .ForMember(dest => dest.TenantName,
                opt => opt.MapFrom(src => src.Tenant.Name));
    }
}