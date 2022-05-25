using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ExchangeDocumentProcessingTask;
using Sungero.Workflow;

namespace Sungero.Exchange.Server
{
  partial class ExchangeDocumentProcessingTaskRouteHandlers
  {

    public virtual void StartReviewAssignment3(Sungero.Workflow.IReviewAssignment reviewAssignment)
    {
      
    }

    #region Задание ответственному (Блок 2)
    
    public virtual void StartBlock2(Sungero.Exchange.Server.ExchangeDocumentProcessingAssignmentArguments e)
    {
      // Определить исполнителя.
      if (_obj.Addressee != null)
        e.Block.Performers.Add(_obj.Addressee);
      else
        e.Block.Performers.Add(_obj.Assignee);
      
      // Заполнить поля из задачи.
      e.Block.Box = _obj.Box;
      e.Block.BusinessUnitBox = ExchangeCore.PublicFunctions.BoxBase.GetRootBox(_obj.Box);
      e.Block.Counterparty = _obj.Counterparty;
      e.Block.ExchangeService = _obj.ExchangeService;
      var dateWithUTC = Sungero.Docflow.PublicFunctions.Module.GetDateWithUTCLabel(_obj.IncomeDate.Value);
      
      // Сформировать тему.
      e.Block.Subject = _obj.IsIncoming == true ? 
        ExchangeDocumentProcessingTasks.Resources.AssignmentSubjectFormat(e.Block.Counterparty, e.Block.BusinessUnitBox.BusinessUnit, dateWithUTC, e.Block.ExchangeService) :
        ExchangeDocumentProcessingTasks.Resources.AssignmentSubjectFormat(e.Block.BusinessUnitBox.BusinessUnit, e.Block.Counterparty, dateWithUTC, e.Block.ExchangeService);
      
      var performer = e.Block.Performers.FirstOrDefault();
      
      // Выдать права на вложения исполнителю.
      foreach (var document in _obj.AllAttachments)
      {
        if (!document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, performer))
          document.AccessRights.Grant(performer, DefaultAccessRightsTypes.FullAccess);
        
        // Выдать права на связанные документы.
        var attachedDocument = Sungero.Docflow.OfficialDocuments.As(document);
        var relatedDocuments = attachedDocument.Relations.GetRelatedFrom();
        foreach (var relatedDocument in relatedDocuments)
        {
          if (!relatedDocument.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Read, performer))
            relatedDocument.AccessRights.Grant(performer, DefaultAccessRightsTypes.Read);
        }
      }
      
      // Выдать права на задачу.
      _obj.AccessRights.Grant(performer, DefaultAccessRightsTypes.Change);
    }

    public virtual void StartAssignment2(Sungero.Exchange.IExchangeDocumentProcessingAssignment assignment, Sungero.Exchange.Server.ExchangeDocumentProcessingAssignmentArguments e)
    {
      assignment.Deadline = _obj.Deadline;
      
      // Переадресованное задание должно приходить от последнего исполнителя.
      var lastProcessingAssignment = ExchangeDocumentProcessingAssignments.GetAll().Where(a => Equals(a.Task, assignment.Task) && a.Id != assignment.Id).OrderByDescending(a => a.Created).FirstOrDefault();
      if (lastProcessingAssignment != null)
        assignment.Author = lastProcessingAssignment.Performer;
    }

    public virtual void CompleteAssignment2(Sungero.Exchange.IExchangeDocumentProcessingAssignment assignment, Sungero.Exchange.Server.ExchangeDocumentProcessingAssignmentArguments e)
    {
      if (_obj.IsReadressed != true && assignment.Result == Exchange.ExchangeDocumentProcessingAssignment.Result.ReAddress)
      {
        _obj.IsReadressed = true;
        _obj.Save();
      }
    }

    public virtual void EndBlock2(Sungero.Exchange.Server.ExchangeDocumentProcessingAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion

  }
}