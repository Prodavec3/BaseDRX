using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.MeetingsUI.Server
{
  partial class MinutesFolderHandlers
  {

    public virtual IQueryable<Sungero.Meetings.IMinutes> MinutesDataQuery(IQueryable<Sungero.Meetings.IMinutes> query)
    {
      
      #region Фильтр
      
      if (_filter == null)
        return query;
      
      // Фильтр по участникам совещания.
      var allMyGroups = Recipients.AllRecipientIds;
      if (_filter.President || _filter.Secretary || _filter.Member)
        query = query.Where(p => p.Meeting != null && ((Equals(p.Meeting.President, Users.Current) && _filter.President) ||
                                                       (Equals(p.Meeting.Secretary, Users.Current) && _filter.Secretary) ||
                                                       (_filter.Member && p.Meeting.Members.Any(m => Equals(m.Member, Users.Current) || allMyGroups.Contains(m.Member.Id)))));
      
      // Фильтр по исполнителю.
      var assignees = new List<Sungero.CoreEntities.IUser>();
      if (_filter.ByMe)
        assignees.Add(Users.Current);
      if (_filter.ByMySubordinate)
        assignees.AddRange(Company.Employees.GetAll(e => e.Department.Manager != null && Equals(e.Department.Manager, Users.Current) && !Equals(e, Users.Current)).ToList());
      if (_filter.ByEmployee && _filter.Employee != null)
        assignees.Add(_filter.Employee);
      if (_filter.ByMe || _filter.ByMySubordinate || _filter.ByEmployee)
        query = query.Where(x => Sungero.RecordManagement.ActionItemExecutionTasks.GetAll().Any(p => p.AttachmentDetails.Any(a => a.AttachmentId == x.Id)
                                                                                                && assignees.Contains(p.Assignee)));
      
      // Фильтр по дате документа.
      var periodBegin = Calendar.UserToday.AddDays(-7);
      var periodEnd = Calendar.UserToday.EndOfDay();
      if (_filter.LastWeek)
        periodBegin = Calendar.UserToday.AddDays(-7);
      
      if (_filter.LastMonth)
        periodBegin = Calendar.UserToday.AddDays(-30);
      
      if (_filter.Last90Days)
        periodBegin = Calendar.UserToday.AddDays(-90);
      
      if (_filter.ManualPeriod)
      {
        periodBegin = _filter.DateRangeFrom ?? Calendar.SqlMinValue;
        periodEnd = _filter.DateRangeTo ?? Calendar.SqlMaxValue;
      }
      
      var serverPeriodBegin = Equals(Calendar.SqlMinValue, periodBegin) ? periodBegin : Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(periodBegin);
      var serverPeriodEnd = Equals(Calendar.SqlMaxValue, periodEnd) ? periodEnd : periodEnd.EndOfDay().FromUserTime();
      var clientPeriodEnd = !Equals(Calendar.SqlMaxValue, periodEnd) ? periodEnd.AddDays(1) : Calendar.SqlMaxValue;
      query = query.Where(j => (j.DocumentDate.Between(serverPeriodBegin, serverPeriodEnd) ||
                                j.DocumentDate == periodBegin) && j.DocumentDate != clientPeriodEnd);
      
      return query;
      
      #endregion
    }
  }

  partial class MeetingsUIHandlers
  {
  }
}