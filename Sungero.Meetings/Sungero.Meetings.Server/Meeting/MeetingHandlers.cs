using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Meetings.Meeting;

namespace Sungero.Meetings
{
  partial class MeetingMembersMemberPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> MembersMemberFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      // Нельзя выбрать роль "Все пользователи" и НОР в качестве участников.
      query = query.Where(x => x.Sid != Sungero.Domain.Shared.SystemRoleSid.AllUsers && !Company.BusinessUnits.Is(x));
      return (IQueryable<T>)RecordManagement.PublicFunctions.Module.ObserversFiltering(query);
    }
  }

  partial class MeetingFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null)
        return query;
      
      // Фильтр по состоянию.
      if (_filter.Planing || _filter.CreateDocument || _filter.ExecuteActionItems || _filter.Concluded)
      {
        var minutes = Minuteses.GetAll(d => d.Meeting != null);

        query = query.Where(x => (_filter.Planing && x.DateTime >= Calendar.Now && x.Status == Meeting.Status.Active) ||
                            (_filter.CreateDocument && !minutes.Any(m => Equals(m.Meeting, x)) && x.DateTime < Calendar.Now) ||
                            (_filter.ExecuteActionItems && minutes.Any(m => Equals(m.Meeting, x) && m.ExecutionState == Docflow.OfficialDocument.ExecutionState.OnExecution)) ||
                            (_filter.Concluded && x.DateTime < Calendar.Now));
      }
      
      // Фильтр по участникам
      var currentEmployee = Company.Employees.Current;
      if (_filter.My && currentEmployee != null)
      {
        var employeeIds = Company.Employees.OwnRecipientIds;
        query = query.Where(x => Equals(x.President, currentEmployee) ||
                            Equals(x.Secretary, currentEmployee) ||
                            x.Members.Any(m => employeeIds.Contains(m.Member.Id)));
      }
      
      if (_filter.ShowEmployee)
      {
        var employee = _filter.Employee;
        if (employee != null)
        {
          var employeeIds = Company.Employees.OwnRecipientIdsFor(employee);
          query = query.Where(x => Equals(x.President, employee) ||
                              Equals(x.Secretary, employee) ||
                              x.Members.Any(m => employeeIds.Contains(m.Member.Id)));
        }
      }
      
      // Фильтр по дате проведения.
      DateTime? periodBegin = null;
      DateTime? periodEnd = null;
      if (_filter.CurrentWeek)
      {
        periodBegin = Calendar.UserToday.BeginningOfWeek();
        periodEnd = Calendar.UserToday.EndOfWeek();
      }
      
      if (_filter.CurrentMounth)
      {
        periodBegin = Calendar.UserToday.BeginningOfMonth();
        periodEnd = Calendar.UserToday.EndOfMonth();
      }
      
      if (_filter.ShowPeriod)
      {
        if (_filter.DateRangeFrom.HasValue)
          periodBegin = _filter.DateRangeFrom.Value;
        if (_filter.DateRangeTo.HasValue)
          periodEnd = _filter.DateRangeTo.Value;
      }
      
      if (periodBegin != null)
      {
        periodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(periodBegin.Value);
        query = query.Where(x => x.DateTime >= periodBegin);
      }
      if (periodEnd != null)
      {
        periodEnd = periodEnd.Value.EndOfDay().FromUserTime();
        query = query.Where(x => x.DateTime <= periodEnd);
      }

      return query;
    }
  }

  partial class MeetingServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      var currentEmployee = Sungero.Company.Employees.Current;
      if (currentEmployee != null && currentEmployee.IsSystem != true && !_obj.State.IsCopied)
        _obj.Secretary = currentEmployee;
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.State.Properties.Name.IsChanged || _obj.State.Properties.DateTime.IsChanged)
      {
        using (TenantInfo.Culture.SwitchTo())
        {
          var dateTime = _obj.DateTime.Value;
          var displayNameWithoutTime = Meetings.Resources.MeetingDisplayValueWithDateFormat(_obj.Name, dateTime.ToUserTime().ToShortDateString());

          _obj.DisplayName = Calendar.HasTime(dateTime)
            ? string.Format("{0} {1}", displayNameWithoutTime, dateTime.ToUserTime().ToShortTimeString())
            : displayNameWithoutTime;
        }
      }

      if (!string.IsNullOrEmpty(_obj.Name))
        _obj.Name = _obj.Name.Trim();
      if (!string.IsNullOrEmpty(_obj.Location))
        _obj.Location = _obj.Location.Trim();
      if (!string.IsNullOrEmpty(_obj.Note))
        _obj.Note = _obj.Note.Trim();
      
      if (string.IsNullOrWhiteSpace(_obj.Name))
        e.AddError(_obj.Info.Properties.Name, Sungero.Meetings.Meetings.Resources.EmptyMeetingSubject);
    }
    
    public override void Saving(Sungero.Domain.SavingEventArgs e)
    {
      // Выдать права на совещание.
      var secretary = _obj.Secretary;
      if (secretary != null && !_obj.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Change, secretary))
        _obj.AccessRights.Grant(secretary, DefaultAccessRightsTypes.Change);
      
      var president = _obj.President;
      if (president != null && !_obj.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Change, president))
        _obj.AccessRights.Grant(president, DefaultAccessRightsTypes.Change);

      var members = _obj.Members.Select(m => m.Member).ToList();
      foreach (var member in members)
        if (!_obj.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Read, member))
          _obj.AccessRights.Grant(member, DefaultAccessRightsTypes.Read);

      // Выдать права на документы.
      var documents = Functions.Meeting.GetMeetingDocuments(_obj, Docflow.PublicConstants.Module.AddendumRelationName);
      foreach (var document in documents)
        PublicFunctions.Meeting.SetAccessRightsOnDocument(_obj, document);
    }
  }

}