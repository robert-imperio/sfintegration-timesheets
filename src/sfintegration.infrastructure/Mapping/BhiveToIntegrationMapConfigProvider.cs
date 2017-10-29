using AutoMapper;
using System;

namespace sfintegration.infrastructure.Mapping
{
    public static class BhiveToIntegrationMapConfigProvider
    {
        public static MapperConfiguration UserTimeClockConfiguration(DateTime startDate)
        {
            return new MapperConfiguration(config =>
            {
                config.CreateMap<bhive.entities.UserTimeClock, sfintegration.entities.UserTimeClock>()
                .ForMember(dst => dst.ProjectId, opts => opts.MapFrom(src => src.JobOrder.ProjectId))
                .ForMember(dst => dst.StartDate, opts => opts.MapFrom(src => startDate));
            });            
        }

    }
}
