using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalTask;

namespace Sungero.Docflow
{

  partial class ApprovalTaskSharedHandlers
  {

    public virtual void AddendaGroupCreated(Sungero.Workflow.Interfaces.AttachmentCreatedEventArgs e)
    {
      var addendum = OfficialDocuments.As(e.Attachment);
      if (addendum == null)
        return;
      
      Functions.ApprovalTask.AddedAddendaAppend(_obj, addendum);
      Functions.ApprovalTask.RemovedAddendaRemove(_obj, addendum);
    }

    public virtual void AddendaGroupDeleted(Sungero.Workflow.Interfaces.AttachmentDeletedEventArgs e)
    {
      var addendum = OfficialDocuments.As(e.Attachment);
      if (addendum == null)
        return;
      
      Functions.ApprovalTask.RemovedAddendaAppend(_obj, addendum);
      Functions.ApprovalTask.AddedAddendaRemove(_obj, addendum);
      
      Functions.ApprovalTask.RefreshApprovalTaskForm(_obj, true);
    }

    public virtual void AddendaGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      var addendum = OfficialDocuments.As(e.Attachment);
      if (addendum == null)
        return;
      
      Functions.ApprovalTask.AddedAddendaAppend(_obj, addendum);
      Functions.ApprovalTask.RemovedAddendaRemove(_obj, addendum);
      
      Functions.ApprovalTask.RefreshApprovalTaskForm(_obj, true);
    }
    
    public virtual void SignatoryChanged(Sungero.Docflow.Shared.ApprovalTaskSignatoryChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      bool skipRefreshEvents = false;
      e.Params.TryGetValue(Constants.ApprovalTask.RefreshApprovalTaskForm.SkipRefreshEventsParamName, out skipRefreshEvents);
      
      if (!skipRefreshEvents)
      {
        var stages = Functions.ApprovalTask.Remote.GetBaseStages(_obj).BaseStages;
        Functions.ApprovalTask.RefreshApprovalTaskForm(_obj, stages, true);
        Functions.ApprovalTask.Remote.UpdateReglamentApprovers(_obj, _obj.ApprovalRule, stages);
      }
    }

    public virtual void StageNumberChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
      // Добавить в лог запись о предыдущем и новом номере этапа для упрощения анализа логов задачи.
      Logger.DebugFormat("Task:{0}. Stage number changed from {1} to {2}", _obj.Id, (e.OldValue ?? 0).ToString(), (e.NewValue ?? 0).ToString());
    }

    public virtual void ExchangeServiceChanged(Sungero.Docflow.Shared.ApprovalTaskExchangeServiceChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      // Обновить предметное отображение регламента.
      _obj.State.Controls.Control.Refresh();
    }

    public virtual void DeliveryMethodChanged(Sungero.Docflow.Shared.ApprovalTaskDeliveryMethodChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      if (e.NewValue == null || e.NewValue.Sid != Constants.MailDeliveryMethod.Exchange)
      {
        _obj.ExchangeService = null;
        _obj.State.Properties.ExchangeService.IsEnabled = false;
      }
      else
      {
        _obj.State.Properties.ExchangeService.IsEnabled = true;
        _obj.ExchangeService = Functions.ApprovalTask.Remote.GetExchangeServices(_obj).DefaultService;
      }
      
      bool skipRefreshEvents = false;
      e.Params.TryGetValue(Constants.ApprovalTask.RefreshApprovalTaskForm.SkipRefreshEventsParamName, out skipRefreshEvents);
      
      if (!skipRefreshEvents)
      {
        var stages = Functions.ApprovalTask.Remote.GetBaseStages(_obj).BaseStages;
        Functions.ApprovalTask.RefreshApprovalTaskForm(_obj, stages, true);
        Functions.ApprovalTask.Remote.UpdateReglamentApprovers(_obj, _obj.ApprovalRule);
        // Обновить предметное отображение регламента.
        _obj.State.Controls.Control.Refresh();

      }
    }
    
    public virtual void AddresseeChanged(Sungero.Docflow.Shared.ApprovalTaskAddresseeChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      var firstAddressee = _obj.Addressees.OrderBy(a => a.Id).FirstOrDefault(a => a.Addressee != null);
      if (firstAddressee == null ||
          firstAddressee != null && !Equals(e.NewValue, firstAddressee.Addressee))
        Functions.ApprovalTask.ClearAddresseesAndFillFirstAddressee(_obj);
      
      bool skipRefreshEvents = false;
      e.Params.TryGetValue(Constants.ApprovalTask.RefreshApprovalTaskForm.SkipRefreshEventsParamName, out skipRefreshEvents);
      
      if (!skipRefreshEvents)
      {
        var stages = Functions.ApprovalTask.Remote.GetBaseStages(_obj).BaseStages;
        Functions.ApprovalTask.RefreshApprovalTaskForm(_obj, stages, true);
        // Обновить обязательных согласующих.
        Sungero.Docflow.Functions.ApprovalTask.Remote.UpdateReglamentApprovers(_obj, _obj.ApprovalRule, stages);
      }
    }
    
    public virtual void ReqApproversChanged(Sungero.Domain.Shared.CollectionPropertyChangedEventArgs e)
    {
      var shadowCopy = _obj.ReqApprovers.ToList();
      var distinctApprovers = shadowCopy.GroupBy(a => a.Approver).Select(a => a.First());
      foreach (var item in shadowCopy.Except(distinctApprovers))
      {
        _obj.ReqApprovers.Remove(item);
      }
    }
    
    public override void AuthorChanged(Sungero.Workflow.Shared.TaskAuthorChangedEventArgs e)
    {
      if (_obj.ApprovalRule != null)
      {
        _obj.ReqApprovers.Clear();
        
        var stages = Functions.ApprovalTask.Remote.GetStages(_obj).Stages;
        var managerStage = stages.Where(s => s.Stage.StageType == Docflow.ApprovalStage.StageType.Manager).FirstOrDefault();
        if (managerStage != null)
        {
          var manager = Docflow.PublicFunctions.ApprovalStage.Remote.GetRemoteStagePerformer(_obj, managerStage.Stage);
          if (manager != null && !manager.Equals(_obj.Author))
            _obj.ReqApprovers.AddNew().Approver = manager;
        }
        
        var reglamentApprovers = stages
          .Where(s => s.Stage.StageType == Docflow.ApprovalStage.StageType.Approvers)
          .OrderBy(num => num.Number)
          .SelectMany(p => p.Stage.Recipients)
          .Select(p => p.Recipient)
          .ToList();

        foreach (var approver in reglamentApprovers)
          _obj.ReqApprovers.AddNew().Approver = approver;
      }
    }
    
    public virtual void ApprovalRuleChanged(Sungero.Docflow.Shared.ApprovalTaskApprovalRuleChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      bool skipRefreshEvents = false;
      e.Params.TryGetValue(Constants.ApprovalTask.RefreshApprovalTaskForm.SkipRefreshEventsParamName, out skipRefreshEvents);
      
      if (!skipRefreshEvents)
        e.Params.AddOrUpdate(Constants.ApprovalTask.RefreshApprovalTaskForm.SkipRefreshEventsParamName, true);
      
      // Очистить на клиенте, т.к. с сервера изменения могут прийти позже.
      _obj.Signatory = null;
      
      if (!skipRefreshEvents)
      {
        var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
        var memo = document != null ? Memos.As(document) : null;
        
        if (memo != null)
          Functions.ApprovalTask.SychronizeMemoAddressees(_obj, memo);
        
        var stages = Functions.ApprovalTask.Remote.GetBaseStages(_obj).BaseStages;
        Functions.ApprovalTask.Remote.ApprovalRuleChanged(_obj, e.NewValue, stages);
        
        e.Params.AddOrUpdate(Constants.ApprovalTask.RefreshApprovalTaskForm.SkipRefreshEventsParamName, false);
        
        Functions.ApprovalTask.RefreshApprovalTaskForm(_obj, true);
      }
    }

    public virtual void DocumentGroupDeleted(Sungero.Workflow.Interfaces.AttachmentDeletedEventArgs e)
    {
      Functions.ApprovalTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      
      // Очистить группу дополнительно.
      var document = OfficialDocuments.As(e.Attachment);
      if (OfficialDocuments.Is(document))
        Functions.OfficialDocument.RemoveRelatedDocumentsFromAttachmentGroup(OfficialDocuments.As(document), _obj.OtherGroup);

      _obj.Subject = Docflow.Resources.AutoformatTaskSubject;
      _obj.DeliveryMethod = null;
      
      Functions.ApprovalTask.RefreshApprovalTaskForm(_obj, true);
    }

    public virtual void DocumentGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      var taskParams = ((Domain.Shared.IExtendedEntity)_obj).Params;
      taskParams[Constants.ApprovalTask.RefreshApprovalTaskForm.SkipRefreshEventsParamName] = true;
      
      var document = _obj.DocumentGroup.OfficialDocuments.First();

      using (TenantInfo.Culture.SwitchTo())
        _obj.Subject = Functions.Module.TrimSpecialSymbols(ApprovalTasks.Resources.TaskSubject, document.Name);
      
      if (!_obj.State.IsCopied)
      {
        Functions.ApprovalTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
        Functions.OfficialDocument.AddRelatedDocumentsToAttachmentGroup(document, _obj.OtherGroup);
      }

      var needClearApprovalRule = false;
      var defaultApprovalRule = Functions.OfficialDocument.Remote.GetDefaultApprovalRule(document);
      if (defaultApprovalRule != null)
        _obj.ApprovalRule = defaultApprovalRule;
      else
        needClearApprovalRule = true;

      if (needClearApprovalRule && _obj.ApprovalRule != null)
        _obj.ApprovalRule = null;
      
      _obj.DocumentExternalApprovalState = document.ExternalApprovalState ?? ApprovalTask.DocumentExternalApprovalState.Empty;
      
      Functions.OfficialDocument.DocumentAttachedInMainGroup(document, _obj);
      
      // Заполнить адресатов из документа.
      var memo = Memos.As(document);
      if (memo != null)
        Functions.ApprovalTask.SychronizeMemoAddressees(_obj, memo);
      
      var stages = Functions.ApprovalTask.Remote.GetBaseStages(_obj).BaseStages;
      Functions.ApprovalTask.Remote.ApprovalRuleChanged(_obj, _obj.ApprovalRule, stages);
      
      taskParams[Constants.ApprovalTask.RefreshApprovalTaskForm.SkipRefreshEventsParamName] = false;
      Functions.ApprovalTask.RefreshApprovalTaskForm(_obj, true);
    }

    public override void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      // TODO: удалить код после исправления бага 17930 (сейчас этот баг в TFS недоступен, он про автоматическое обрезание темы).
      if (e.NewValue != null && e.NewValue.Length > ApprovalTasks.Info.Properties.Subject.Length)
        _obj.Subject = e.NewValue.Substring(0, ApprovalTasks.Info.Properties.Subject.Length);
      
      if (string.IsNullOrWhiteSpace(e.NewValue))
        _obj.Subject = Docflow.Resources.AutoformatTaskSubject;
    }

    public virtual void AddApproversChanged(Sungero.Domain.Shared.CollectionPropertyChangedEventArgs e)
    {
      _obj.State.Controls.Control.Refresh();
    }
  }
}