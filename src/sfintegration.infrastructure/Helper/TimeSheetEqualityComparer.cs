using System;
using System.Linq;
using System.Collections.Generic;
using sfintegration.entities;

namespace sfintegration.infrastructure.Helper
{
    public class TimeSheetEqualityComparer : IEqualityComparer<entities.TimeSheet>
    {
        public bool Equals(TimeSheet x, TimeSheet y)
        {
            return (x.UserId == y.UserId && x.StartDate == y.StartDate);
        }

        public int GetHashCode(TimeSheet obj)
        {
            return obj.UserId.GetHashCode() + obj.StartDate.GetHashCode();
        }
    }
}
