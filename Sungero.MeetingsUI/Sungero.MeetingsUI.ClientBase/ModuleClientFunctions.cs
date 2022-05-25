using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.MeetingsUI.Client
{
  public class ModuleFunctions
  {

    /// <summary>
    /// Отобразить отчет по исполнению поручений по совещаниям.
    /// </summary>
    public virtual void OpenActionItemExecutionReport()
    {
      var actionItemExecutionReport = RecordManagement.Reports.GetActionItemsExecutionReport();
      actionItemExecutionReport.IsMeetingsCoverContext = true;
      actionItemExecutionReport.Open();
    }

    /// <summary>
    /// Создать протокол по совещанию.
    /// </summary>
    public virtual void CreateMinutes()
    {
      Meetings.PublicFunctions.Minutes.Remote.CreateMinutes().Show();
    }

    /// <summary>
    /// Создать повестку.
    /// </summary>
    public virtual void CreateAgenda()
    {
      Meetings.PublicFunctions.Agenda.Remote.CreateAgenda().Show();
    }

    /// <summary>
    /// Создать совещание.
    /// </summary>
    public virtual void CreateMeeting()
    {
      Meetings.PublicFunctions.Meeting.Remote.CreateMeeting().Show();
    }
   
    /// <summary>
    /// Отобразить список поручений по совещаниям.
    /// </summary>
    /// <returns>Список поручений.</returns>
    public virtual IQueryable<RecordManagement.IActionItemExecutionTask> ShowMeetingActionItemExecutionTasks()
    {
      return Meetings.PublicFunctions.Module.Remote.GetMeetingActionItemExecutionTasks();
    }    

  }
}