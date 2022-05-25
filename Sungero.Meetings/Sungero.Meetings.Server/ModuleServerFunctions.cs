using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;

namespace Sungero.Meetings.Server
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Получить список поручений по совещаниям.
    /// </summary>
    /// <returns>Список поручений.</returns>
    [Remote, Public]
    public IQueryable<RecordManagement.IActionItemExecutionTask> GetMeetingActionItemExecutionTasks()
    {
      var minuteses = Minuteses.GetAll(m => m.Meeting != null);
      var groupId = Docflow.PublicConstants.Module.TaskMainGroup.ActionItemExecutionTask;
      return RecordManagement.ActionItemExecutionTasks.GetAll(a => a.AttachmentDetails.Any(d => d.EntityTypeGuid == Minutes.ClassTypeGuid &&
                                                                                           d.GroupId == groupId && minuteses.Any(m => Equals(m.Id, d.AttachmentId))));
    }
    
    /// <summary>
    /// Данные для отчета полномочий сотрудника из модуля Совещания.
    /// </summary>
    /// <param name="employee">Сотрудник для обработки.</param>
    /// <returns>Данные для отчета.</returns>
    [Public]
    public virtual List<Company.Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine> GetResponsibilitiesReportData(IEmployee employee)
    {
      // HACK: Получаем отображаемое имя модуля.
      var moduleGuid = new MeetingsModule().Id;
      var moduleName = Sungero.Metadata.Services.MetadataSearcher.FindModuleMetadata(moduleGuid).GetDisplayName();
      var modulePriority = Company.PublicConstants.ResponsibilitiesReport.MeetingsPriority;
      var result = new List<Company.Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine>();
      
      if (!Meetings.AccessRights.CanRead())
        return result;
      
      var emplIsPresident = Meetings.GetAll(x => Equals(x.President, employee))
        .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active)
        .Where(d => d.DateTime >= Calendar.Now);
      result = Company.PublicFunctions.Module.AppendResponsibilitiesReportResult(result, emplIsPresident, moduleName, modulePriority,
                                                                                 Resources.MeetingsPresident, null);
      
      var emplIsSecretary = Meetings.GetAll(x => Equals(x.Secretary, employee))
        .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active)
        .Where(d => d.DateTime >= Calendar.Now);
      result = Company.PublicFunctions.Module.AppendResponsibilitiesReportResult(result, emplIsSecretary, moduleName, modulePriority,
                                                                                 Resources.MeetingsSecretary, null);
      
      return result;
    }
  }
}