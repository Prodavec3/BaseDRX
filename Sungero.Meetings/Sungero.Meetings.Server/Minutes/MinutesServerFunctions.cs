using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;
using Sungero.Meetings.Minutes;
using Sungero.Metadata;

namespace Sungero.Meetings.Server
{
  partial class MinutesFunctions
  {
    /// <summary>
    /// Заполнение свойств поручения, созданного по документу.
    /// </summary>
    /// <param name="actionItem">Поручение, созданное по документу.</param>
    public override void FillActionItemExecutionTaskOnCreatedFromDocument(RecordManagement.IActionItemExecutionTask actionItem)
    {
      var currentEmployee = Company.Employees.Current;
      if (_obj.Meeting != null)
      {
        actionItem.AssignedBy = currentEmployee;
        if (_obj.Meeting.President != null)
        {
          var secretary = Docflow.PublicFunctions.Module.GetSecretary(_obj.Meeting.President);
          var substitutedEmployees = Substitutions.ActiveSubstitutedUsers.Where(asu => Company.Employees.Is(asu))
            .Select(asu => Company.Employees.As(asu))
            .ToList();
          
          if (Equals(secretary, currentEmployee) || substitutedEmployees.Contains(_obj.Meeting.President))
            actionItem.AssignedBy = _obj.Meeting.President;
          else if (!Equals(_obj.Meeting.President, currentEmployee) && !Equals(_obj.Meeting.President, actionItem.Assignee) &&
                   !actionItem.CoAssignees.Any(x => Equals(_obj.Meeting.President, x.Assignee)))
            actionItem.ActionItemObservers.AddNew().Observer = _obj.Meeting.President;
        }
      }
    }
    
    /// <summary>
    /// Создать протокол.
    /// </summary>
    /// <returns>Протокол.</returns>
    [Public, Remote]
    public static IMinutes CreateMinutes()
    {
      return Minuteses.Create();
    }
    
    /// <summary>
    /// Получить список участников совещания для краткого протокола.
    /// </summary>
    /// <param name="minutes">Протокол.</param>
    /// <returns>Список участников совещания.</returns>
    [Converter("GetMeetingMembersShortMinutes")]
    public static string GetMeetingMembersShortMinutes(IMinutes minutes)
    {
      if (minutes.Meeting == null)
        return null;
      
      return PublicFunctions.Meeting.Remote.GetMeetingMembersString(minutes.Meeting, false, false);
    }
    
    /// <summary>
    /// Получить список участников совещания для полного протокола.
    /// </summary>
    /// <param name="minutes">Протокол.</param>
    /// <returns>Список участников совещания.</returns>
    [Converter("GetMeetingMembersFullMinutes")]
    public static string GetMeetingMembersFullMinutes(IMinutes minutes)
    {
      if (minutes.Meeting == null)
        return null;
      
      return PublicFunctions.Meeting.Remote.GetMeetingMembersString(minutes.Meeting, true, false);
    }
    
    /// <summary>
    /// Получить список из председателя, секретаря и участников совещания с должностями.
    /// </summary>
    /// <param name="minutes">Протокол.</param>
    /// <returns>Список из председателя, секретаря и участников совещания с должностями.</returns>
    [Converter("GetMeetingChairpersonSecretaryMembersWithJobTitle")]
    public static string GetMeetingChairpersonSecretaryMembersWithJobTitle(IMinutes minutes)
    {
      if (minutes.Meeting == null)
        return null;
      
      return PublicFunctions.Meeting.Remote.GetMeetingMembersString(minutes.Meeting, false, true);
    }
    
    /// <summary>
    /// Получить список участников совещания с должностями.
    /// </summary>
    /// <param name="minutes">Протокол.</param>
    /// <returns>Список участников совещания с должностями.</returns>
    [Converter("GetMeetingMembersWithJobTitle")]
    public static string GetMeetingMembersWithJobTitle(IMinutes minutes)
    {
      if (minutes.Meeting == null)
        return null;
      
      return PublicFunctions.Meeting.Remote.GetMeetingMembersString(minutes.Meeting, true, true);
    }
    
    /// <summary>
    /// Получить председателя совещания.
    /// </summary>
    /// <param name="minutes">Протокол.</param>
    /// <returns>Председатель совещания.</returns>
    [Converter("GetMeetingPresident")]
    public static Company.IEmployee GetMeetingPresident(IMinutes minutes)
    {
      if (minutes.Meeting != null && minutes.Meeting.President != null)
        return minutes.Meeting.President;
      
      return minutes.OurSignatory;
    }
    
    /// <summary>
    /// Получить секретаря совещания.
    /// </summary>
    /// <param name="minutes">Протокол.</param>
    /// <returns>Секретарь совещания.</returns>
    [Converter("GetMeetingSecretary")]
    public static Company.IEmployee GetMeetingSecretary(IMinutes minutes)
    {
      if (minutes.Meeting != null && minutes.Meeting.Secretary != null)
        return minutes.Meeting.Secretary;

      return minutes.PreparedBy;
    }
    
    /// <summary>
    /// Получить наименование совещания.
    /// </summary>
    /// <param name="minutes">Протокол.</param>
    /// <returns>Наименование совещания.</returns>
    [Converter("GetMeetingName")]
    public static string GetMeetingName(IMinutes minutes)
    {
      if (minutes.Meeting != null)
        return minutes.Meeting.Name.Trim();
      
      if (!string.IsNullOrEmpty(minutes.Subject))
        return minutes.Subject;
      
      return minutes.Name.Trim();
    }
    
  }
}