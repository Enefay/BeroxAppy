using AutoMapper;
using BeroxAppy.Customers;
using BeroxAppy.Employees;
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


        /* Employee mappings */
        CreateMap<Employee, EmployeeDto>()
            .Ignore(x => x.Password) // Create için gerekli ama entity'de yok
            .Ignore(x => x.UserName) // Create için gerekli ama entity'de yok
            .Ignore(x => x.FullName) // Manuel doldurulacak
            .Ignore(x => x.EmployeeTypeDisplay) // Manuel doldurulacak
            .Ignore(x => x.ServiceGenderDisplay) // Manuel doldurulacak
            .Ignore(x => x.HasUser) // Manuel doldurulacak
            .Ignore(x => x.UserStatus); // Manuel doldurulacak

        CreateMap<EmployeeDto, Employee>()
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
            .Ignore(x => x.ReservationDetails) // Navigation property
            .Ignore(x => x.EmployeeServices) // Navigation property
            .Ignore(x => x.WorkingHours); // Navigation property

        /* EmployeeService mappings */
        CreateMap<EmployeeService, EmployeeServiceAssignmentDto>()
            .Ignore(x => x.ServiceTitle) // Manuel doldurulacak
            .Ignore(x => x.ServiceCategoryName) // Manuel doldurulacak
            .Ignore(x => x.ServicePrice) // Manuel doldurulacak
            .Ignore(x => x.ServiceDuration); // Manuel doldurulacak

        /* EmployeeWorkingHours mappings */
        CreateMap<EmployeeWorkingHours, EmployeeWorkingHoursDto>()
            .Ignore(x => x.DayOfWeekDisplay) // Manuel doldurulacak
            .Ignore(x => x.WorkingHoursDisplay) // Manuel doldurulacak
            .Ignore(x => x.BreakDisplay); // Manuel doldurulacak

        CreateMap<EmployeeWorkingHoursDto, EmployeeWorkingHours>()
            .Ignore(x => x.Id)
            .Ignore(x => x.Employee); // Navigation property

    }
}
