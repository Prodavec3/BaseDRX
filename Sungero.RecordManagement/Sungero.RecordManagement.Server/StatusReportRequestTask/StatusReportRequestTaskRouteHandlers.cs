using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.StatusReportRequestTask;

namespace Sungero.RecordManagement.Server
{
  partial class StatusReportRequestTaskRouteHandlers
  {

    #region -  3 - Отчитаться по поручению
    
    public virtual void StartBlock3(Sungero.RecordManagement.Server.ReportRequestAssignmentArguments e)
    {
      // Задать тему, срок и исполнителей.
      if (string.IsNullOrEmpty(_obj.ReportNote))
        e.Block.Subject = Functions.StatusReportRequestTask.GetStatusReportRequestSubject(_obj, StatusReportRequestTasks.Resources.ProvideReportByJob);
      else
      {
        e.Block.Subject = Functions.StatusReportRequestTask.GetStatusReportRequestSubject(_obj, StatusReportRequestTasks.Resources.FinalizeReportByJob);
        e.Block.IsRework = true;
      }
      
      if (_obj.MaxDeadline.HasValue)
        e.Block.AbsoluteDeadline = _obj.MaxDeadline.Value;
      
      e.Block.Performers.Add(_obj.Assignee);
      
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault());
    }
    
    public virtual void StartAssignment3(Sungero.RecordManagement.IReportRequestAssignment assignment, Sungero.RecordManagement.Server.ReportRequestAssignmentArguments e)
    {
      assignment.ActiveText = _obj.Report;
      
      // Выдать права на изменение для возможности прекращения подзадач.
      Functions.ActionItemExecutionTask.GrantAccessRightToAssignment(assignment, _obj);
    }
    
    public virtual void CompleteAssignment3(Sungero.RecordManagement.IReportRequestAssignment assignment, Sungero.RecordManagement.Server.ReportRequestAssignmentArguments e)
    {
      _obj.Report = assignment.ActiveText;
    }
    
    public virtual void EndBlock3(Sungero.RecordManagement.Server.ReportRequestAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion

    #region -  4 - Принять отчет

    public virtual void StartBlock4(Sungero.RecordManagement.Server.ReportRequestCheckAssignmentArguments e)
    {
      var subject = Functions.StatusReportRequestTask.GetStatusReportRequestSubject(_obj, StatusReportRequestTasks.Resources.CheckReportJob);
      e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(subject);
      e.Block.Performers.Add(_obj.StartedBy);
      e.Block.RelativeDeadlineHours = 8;
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault());
    }
    
    public virtual void StartAssignment4(Sungero.RecordManagement.IReportRequestCheckAssignment assignment, Sungero.RecordManagement.Server.ReportRequestCheckAssignmentArguments e)
    {
      assignment.Author = _obj.Assignee;
      
      // Выдать права на изменение для возможности прекращения подзадач.
      Functions.ActionItemExecutionTask.GrantAccessRightToAssignment(assignment, _obj);
    }
    
    public virtual void CompleteAssignment4(Sungero.RecordManagement.IReportRequestCheckAssignment assignment, Sungero.RecordManagement.Server.ReportRequestCheckAssignmentArguments e)
    {
      // Обновить срок запроса отчета.
      if (assignment.Result == Sungero.RecordManagement.ReportRequestCheckAssignment.Result.ForRework)
      {
        var deadlineInHours = 8;
        _obj.MaxDeadline = Calendar.Now.AddWorkingHours(_obj.Assignee, deadlineInHours);
      }
      
      // Вернуть комментарий к отчету.
      _obj.ReportNote = assignment.ActiveText;
    }

    public virtual void EndBlock4(Sungero.RecordManagement.Server.ReportRequestCheckAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    public virtual void StartReviewAssignment99(Sungero.Workflow.IReviewAssignment reviewAssignment)
    {
      
    }
    
  }
}