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
            .ForMember(dest => dest.ReservationDetails, opt => opt.MapFrom(src => src.ReservationDetails)); // Child map varsa
        CreateMap<ReservationDetailViewModel, CreateReservationDetailDto>();
    }
}
