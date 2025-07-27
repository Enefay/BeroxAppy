using AutoMapper;
using BeroxAppy.Reservations;
using static BeroxAppy.Web.Pages.Reservations.CreateEditModalModel;

namespace BeroxAppy.Web;

public class BeroxAppyWebAutoMapperProfile : Profile
{
    public BeroxAppyWebAutoMapperProfile()
    {
        // Rezervasyon
        CreateMap<ReservationViewModel, CreateReservationDto>()
            .ForMember(dest => dest.ReservationDetails, opt => opt.MapFrom(src => src.ReservationDetails));

        CreateMap<ReservationViewModel, UpdateReservationDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id)) // ID mapping ekleyin
            .ForMember(dest => dest.ReservationDetails, opt => opt.MapFrom(src => src.ReservationDetails));

        CreateMap<ReservationDetailViewModel, UpdateReservationDetaillDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id)); // ID mapping ekleyin

        CreateMap<UpdateReservationDetaillDto, ReservationDetailViewModel>();
        CreateMap<ReservationDetailViewModel, CreateReservationDetailDto>();
        CreateMap<CreateReservationDetailDto, ReservationDetailViewModel>();

        CreateMap<ReservationDto, ReservationViewModel>()
            .ForMember(dest => dest.ReservationDetails, opt => opt.MapFrom(src => src.ReservationDetails));

        CreateMap<ReservationDetailDto, ReservationDetailViewModel>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.EmployeeName))
            .ForMember(dest => dest.ServiceTitle, opt => opt.MapFrom(src => src.ServiceTitle));
    }
}
