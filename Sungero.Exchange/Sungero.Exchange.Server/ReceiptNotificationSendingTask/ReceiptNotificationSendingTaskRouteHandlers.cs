using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ReceiptNotificationSendingTask;
using Sungero.Workflow;

namespace Sungero.Exchange.Server
{
  partial class ReceiptNotificationSendingTaskRouteHandlers
  {

    public virtual void StartReviewAssignment3(Sungero.Workflow.IReviewAssignment reviewAssignment)
    {
      
    }

    #region Задание ответственному (Блок 2)
    
    public virtual void StartBlock2(Sungero.Exchange.Server.ReceiptNotificationSendingAssignmentArguments e)
    {
      // Определить исполнителя.
      var performer = _obj.Addressee != null ? _obj.Addressee : _obj.Box.Responsible;
      e.Block.Performers.Add(performer);

      // Заполнить поля из задачи.
      e.Block.Subject = ReceiptNotificationSendingTasks.Resources.AssignmentSubjectFormat(_obj.Box.Name);
      e.Block.Box = _obj.Box;
      e.Block.RelativeDeadlineHours = 4;
      
      // Выдать права на задачу.
      _obj.AccessRights.Grant(performer, DefaultAccessRightsTypes.Change);
    }

    public virtual void StartAssignment2(Sungero.Exchange.IReceiptNotificationSendingAssignment assignment, Sungero.Exchange.Server.ReceiptNotificationSendingAssignmentArguments e)
    {
      _obj.MaxDeadline = assignment.Deadline;
      
      // Переадресованное задание должно приходить от последнего исполнителя.
      var lastProcessingAssignment = ReceiptNotificationSendingAssignments.GetAll().Where(a => Equals(a.Task, assignment.Task) && a.Id != assignment.Id).OrderByDescending(a => a.Created).FirstOrDefault();
      if (lastProcessingAssignment != null)
        assignment.Author = lastProcessingAssignment.Performer;
    }

    public virtual void CompleteAssignment2(Sungero.Exchange.IReceiptNotificationSendingAssignment assignment, Sungero.Exchange.Server.ReceiptNotificationSendingAssignmentArguments e)
    {
      
    }

    public virtual void EndBlock2(Sungero.Exchange.Server.ReceiptNotificationSendingAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion
  }
}