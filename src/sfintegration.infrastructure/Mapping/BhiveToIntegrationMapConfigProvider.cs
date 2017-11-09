using AutoMapper;
using System;
using sfintegration.infrastructure.Extension;
using sfintegration.infrastructure.Helper;

namespace sfintegration.infrastructure.Mapping
{
    public static class BhiveToIntegrationMapConfigProvider
    {
        public static MapperConfiguration UserTimeClockConfiguration()
        {
            return new MapperConfiguration(config =>
            {
                config.CreateMap<bhive.entities.UserTimeClock, sfintegration.entities.UserTimeClock>()
                .ForMember(dst => dst.ProjectId, opts => opts.MapFrom(src => src.JobOrder.ProjectId))
                .ForMember(dst => dst.StartDate, opts => opts.MapFrom(src => GetStartOfWorkWeek(src.TimeZoneId, src.StartTime)));
            });            
        }

        private static DateTime GetStartOfWorkWeek(string abbreviatedTimeZoneId, DateTime dateTime)
        {
            var dt = TimeZoneHelper.Instance.ConvertFromUTC(dateTime, abbreviatedTimeZoneId);

            return dt.StartOfWorkWeek();
        }

    }
}
