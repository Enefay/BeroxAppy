using AutoMapper;
using BeroxAppy.Services;

namespace BeroxAppy;

public class BeroxAppyApplicationAutoMapperProfile : Profile
{
    public BeroxAppyApplicationAutoMapperProfile()
    {
        CreateMap<ServiceCategory, ServiceCategoryDto>().ReverseMap();
        CreateMap<Service, ServiceDto>().ReverseMap();

    }
}
