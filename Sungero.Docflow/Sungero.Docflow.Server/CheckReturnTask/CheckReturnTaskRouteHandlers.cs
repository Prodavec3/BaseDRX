using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.CheckReturnTask;
using Sungero.Workflow;

namespace Sungero.Docflow.Server
{
  partial class CheckReturnTaskRouteHandlers
  {

    #region Отсрочка создания задания (блок 2)
    
    public virtual void StartBlock2(Sungero.Workflow.Server.Route.MonitoringStartBlockEventArguments e)
    {
      
    }
    
    public virtual bool Monitoring2Result()
    {
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      var tracking = document.Tracking.Where(r => Equals(r.ReturnTask, _obj)).FirstOrDefault();
      
      Logger.DebugFormat("ReturnControlMonitoring: Task {0}, AssignmentStartDate {1}, tracking is not null {2} and row date = {3}",
                     _obj.Id, _obj.AssignmentStartDate, tracking != null, tracking != null ? tracking.ReturnDate : DateTime.MinValue);
      
      return !_obj.AssignmentStartDate.HasValue || _obj.AssignmentStartDate.Value <= Calendar.Today ||
        tracking == null || tracking.ReturnDate != null;
    }
    
    #endregion

    #region Задание на возврат документа (блок 3)
    
    public virtual void StartBlock3(Sungero.Docflow.Server.CheckReturnAssignmentArguments e)
    {
      var tracking = Functions.CheckReturnTask.GetTrackingByTask(_obj);
      
      if (tracking == null)
        return;

      e.Block.Performers.Add(_obj.Assignee);
      
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      e.Block.Subject = tracking.Action == Docflow.OfficialDocumentTracking.Action.Sending ?
        ApprovalTasks.Resources.ControlReturnAsgSubjectFormat(document.Name) :
        CheckReturnTasks.Resources.ReturnAssignmentSubjectFormat(document.Name);
      
      // Выдаем исполнителю права на чтение документа.
      document.AccessRights.Grant(_obj.Assignee, DefaultAccessRightsTypes.Read);
    }

    public virtual void StartAssignment3(Sungero.Docflow.ICheckReturnAssignment assignment, Sungero.Docflow.Server.CheckReturnAssignmentArguments e)
    {
      assignment.Deadline = _obj.Deadline;
      
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      
      // Выполнить задание, если документ уже вернули.
      var tracking = document.Tracking.Where(r => Equals(r.ReturnTask, _obj) && r.ReturnDate != null && r.ReturnResult != null).FirstOrDefault();
      
      if (tracking != null)
        assignment.Complete(Docflow.CheckReturnAssignment.Result.Complete);
    }
    
    public virtual void CompleteAssignment3(Sungero.Docflow.ICheckReturnAssignment assignment, Sungero.Docflow.Server.CheckReturnAssignmentArguments e)
    {
      Functions.CheckReturnTask.SetReturnResult(_obj, assignment.Performer, true);
    }

    public virtual void EndBlock3(Sungero.Docflow.Server.CheckReturnAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region Контроль возврата (блок 4)

    public virtual void StartBlock4(Sungero.Docflow.Server.CheckReturnCheckAssignmentArguments e)
    {
      // Если документ уже возвращен, то задание на контроль возврата не создавать.
      var tracking = Functions.CheckReturnTask.GetTrackingByTask(_obj);
      if (tracking == null || Equals(tracking.ReturnResult, Docflow.OfficialDocumentTracking.ReturnResult.Returned))
        return;
      
      if (!Equals(_obj.Author, _obj.Assignee))
        e.Block.Performers.Add(_obj.Author);
      
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      e.Block.Subject = CheckReturnTasks.Resources.CheckReturnSubjectFormat(document.Name);
      e.Block.RelativeDeadlineDays = 1;
    }

    public virtual void StartAssignment4(Sungero.Docflow.ICheckReturnCheckAssignment assignment, Sungero.Docflow.Server.CheckReturnCheckAssignmentArguments e)
    {

    }

    public virtual void CompleteAssignment4(Sungero.Docflow.ICheckReturnCheckAssignment assignment, Sungero.Docflow.Server.CheckReturnCheckAssignmentArguments e)
    {
      var documentIsReturned = assignment.Result == Docflow.CheckReturnCheckAssignment.Result.Returned;
      Functions.CheckReturnTask.SetReturnResult(_obj, assignment.Performer, documentIsReturned);
    }

    public virtual void EndBlock4(Sungero.Docflow.Server.CheckReturnCheckAssignmentEndBlockEventArguments e)
    {
      
    }

    #endregion

    #region Задание-контроль задачи (блок 99)
    
    public virtual void StartReviewAssignment99(Sungero.Workflow.IReviewAssignment reviewAssignment)
    {
      
    }
    
    #endregion

  }
}