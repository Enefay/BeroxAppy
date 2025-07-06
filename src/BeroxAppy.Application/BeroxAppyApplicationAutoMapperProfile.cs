using AutoMapper;
using BeroxAppy.Customers;
using BeroxAppy.Services;
using Volo.Abp.AutoMapper;

namespace BeroxAppy;

public class BeroxAppyApplicationAutoMapperProfile : Profile
{
    public BeroxAppyApplicationAutoMapperProfile()
    {
        CreateMap<ServiceCategory, ServiceCategoryDto>().ReverseMap();
        CreateMap<Service, ServiceDto>()
            .ReverseMap()
            .ForMember(dest => dest.Category, opt => opt.Ignore());


        /* Customer mappings */
        CreateMap<Customer, CustomerDto>()
            .Ignore(x => x.GenderDisplay) // Manuel doldurulacak
            .Ignore(x => x.Age) // Manuel doldurulacak
            .Ignore(x => x.DebtStatusDisplay); // Manuel doldurulacak

        CreateMap<CustomerDto, Customer>()
            .Ignore(x => x.Id)
            .Ignore(x => x.ExtraProperties)
            .Ignore(x => x.ConcurrencyStamp)
            .Ignore(x => x.CreationTime)
            .Ignore(x => x.CreatorId)
            .Ignore(x => x.LastModificationTime)
            .Ignore(x => x.LastModifierId)
            .Ignore(x => x.IsDeleted)
            .Ignore(x => x.DeleterId)
            .Ignore(x => x.DeletionTime)
            .Ignore(x => x.Reservations) // Navigation property
            .Ignore(x => x.Payments); // Navigation property

    }
}
