using AutoMapper;

namespace sfintegration.infrastructure.Mapping
{
    public static class SalesForceToIntegrationMapConfigProvider
    {
        public static MapperConfiguration Configuration()
        {
            return new MapperConfiguration(config =>
            {
                TimeSheetConfig(config);
                TimeSheetActivityConfig(config);
            });            
        }

        private static void TimeSheetConfig(IMapperConfigurationExpression config)
        {
            config.CreateMap<sfintegration.salesforce.api.client.SFEnterprise.BP_Weekly_Timesheet__c, sfintegration.entities.TimeSheet>()
                .ForMember(dst => dst.Id, opts => opts.MapFrom(src => src.Id))
                .ForMember(dst => dst.Name, opts => opts.MapFrom(src => src.Name))
                .ForMember(dst => dst.UserId, opts => opts.MapFrom(src => src.Submitted_By__c))
                .ForMember(dst => dst.StartDate, opts => opts.MapFrom(src => src.Start_Date__c))
                .ForMember(dst => dst.Status, opts => opts.MapFrom(src => src.Status__c))
                .ForMember(dst => dst.LastUpdated, opts => opts.MapFrom(src => src.LastModifiedDate))
                .ForMember(dst => dst.UpdatedBy, opts => opts.MapFrom(src => (src.LastModifiedBy.FirstName ?? "") + " " + src.LastModifiedBy.LastName));
        }

        private static void TimeSheetActivityConfig(IMapperConfigurationExpression config)
        {
            config.CreateMap<sfintegration.salesforce.api.client.SFEnterprise.BP_Timesheet_Activity__c, sfintegration.entities.TimeSheetActivity>()
                .ForMember(dst => dst.Id, opts => opts.MapFrom(src => src.Id))
                .ForMember(dst => dst.TimeSheetId, opts => opts.MapFrom(src => src.Timesheet_Reference__c))
                .ForMember(dst => dst.UserId, opts => opts.MapFrom(src => src.Timesheet_Reference__r.Submitted_By__c))
                .ForMember(dst => dst.ProjectId, opts => opts.MapFrom(src => src.BP_Project__c))
                .ForMember(dst => dst.JobOrderId, opts => opts.MapFrom(src => src.Job_Order__c))
                .ForMember(dst => dst.ActivityId, opts => opts.MapFrom(src => src.BP_Activity__c))
                .ForMember(dst => dst.StartDate, opts => opts.MapFrom(src => src.Timesheet_Reference__r.Start_Date__c))
                .ForMember(dst => dst.StartTime, opts => opts.MapFrom(src => src.Start_Time__c))
                .ForMember(dst => dst.EndTime, opts => opts.MapFrom(src => src.End_Time__c))
                .ForMember(dst => dst.LastUpdated, opts => opts.MapFrom(src => src.LastModifiedDate))
                .ForMember(dst => dst.UpdatedBy, opts => opts.MapFrom(src => (src.LastModifiedBy.FirstName ?? "") + " " + src.LastModifiedBy.LastName));
        }
    }
}
