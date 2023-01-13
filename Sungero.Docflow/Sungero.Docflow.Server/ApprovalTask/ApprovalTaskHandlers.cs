using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalTask;

namespace Sungero.Docflow
{
  partial class ApprovalTaskAddApproversApproverPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> AddApproversApproverFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = query.Where(c => c.Status == CoreEntities.DatabookEntry.Status.Active);
      
      // Отфильтровать всех пользователей.
      query = query.Where(x => x.Sid != Sungero.Domain.Shared.SystemRoleSid.AllUsers);
      
      // Отфильтровать служебные роли.
      return (IQueryable<T>)RecordManagement.PublicFunctions.Module.ObserversFiltering(query);
    }
  }

  partial class ApprovalTaskExchangeServicePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ExchangeServiceFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var services = Functions.ApprovalTask.GetExchangeServices(_obj).Services;
      query = query.Where(s => services.Contains(s));
      return query;
    }
  }

  partial class ApprovalTaskObserversObserverPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> ObserversObserverFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return (IQueryable<T>)RecordManagement.PublicFunctions.Module.ObserversFiltering(query);
    }
  }

  partial class ApprovalTaskCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.AbortingReason);
      e.Without(_info.Properties.ControlReturnStartDate);
      e.Without(_info.Properties.Iteration);
      e.Without(_info.Properties.StageNumber);
    }
  }

  partial class ApprovalTaskSignatoryPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> SignatoryFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      e.DisableUiFiltering = true;
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();

      if (Functions.OfficialDocument.SignatorySettingWithAllUsersExist(document))
        return query;
      
      var signatories = Functions.OfficialDocument.GetSignatoriesIds(document);
      
      return query.Where(s => signatories.Contains(s.Id));
    }
  }

  partial class ApprovalTaskApprovalRulePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ApprovalRuleFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (document == null)
        return query;
      
      var rules = Functions.OfficialDocument.GetApprovalRules(document).ToList();
      return query.Where(r => rules.Contains(r));
    }
  }

  partial class ApprovalTaskServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      Functions.ApprovalTask.FillAddresseeFromAddressees(_obj);
      
      // Если тема не сформирована, то подставить пустую.
      if (_obj.Subject == Docflow.Resources.AutoformatTaskSubject)
        using (TenantInfo.Culture.SwitchTo())
          _obj.Subject = ApprovalTasks.Resources.TaskSubjectFormat(string.Empty);
      
      // Выдать права на документы для всех, кому выданы права на задачу.
      if (_obj.State.IsChanged)
        Functions.Module.GrantManualReadRightForAttachments(_obj, _obj.AllAttachments.ToList());
    }

    public override void Saving(Sungero.Domain.SavingEventArgs e)
    {
      // Обновить обязательных согласующих.
      Functions.ApprovalTask.UpdateReglamentApprovers(_obj, _obj.ApprovalRule);
    }

    public override void Deleting(Sungero.Domain.DeletingEventArgs e)
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (document == null || Locks.GetLockInfo(document).IsLocked)
        return;
      
      // Удалить записи о выдаче документа (иначе не даст удалить из-за зависимостей).
      var tracking = document.Tracking.Where(r => Equals(r.ReturnTask, _obj)).ToList();
      foreach (var row in tracking)
      {
        row.ReturnTask = null;
        row.ReturnDeadline = null;
      }
    }

    public override void BeforeRestart(Sungero.Workflow.Server.BeforeRestartEventArgs e)
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (document == null)
        return;
      
      var newSubject = string.Empty;
      // Формирование темы задачи в локали тенанта.
      using (TenantInfo.Culture.SwitchTo())
        newSubject = Functions.Module.TrimSpecialSymbols(ApprovalTasks.Resources.TaskSubject, document.Name);
      if (_obj.Subject != newSubject)
        _obj.Subject = newSubject;
      
      // Очистить ожидаемый срок согласования.
      _obj.MaxDeadline = null;
      
      // Заполнить коллекции добавленных и удаленных вручную документов в задаче.
      Functions.ApprovalTask.AddedAddendaAppend(_obj);
      Functions.ApprovalTask.RemovedAddendaAppend(_obj);
      
      // Синхронизация приложений для заполнения коллекции добавленных и удаленных вручную документов.
      Functions.ApprovalTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
    }

    public override void BeforeAbort(Sungero.Workflow.Server.BeforeAbortEventArgs e)
    {
      // Если прекращён черновик, прикладную логику по прекращению выполнять не надо.
      if (_obj.State.Properties.Status.OriginalValue == Workflow.Task.Status.Draft)
        return;
      
      Functions.ApprovalTask.SendApprovalAbortNotice(_obj);
      
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      
      // Обновить статус согласования - аннулирован и сделать его устаревшим при необходимости.
      if (document != null)
      {
        bool setObsolete;
        e.Params.TryGetValue(Constants.ApprovalTask.NeedSetDocumentObsolete, out setObsolete);
        
        var needSetState = false;
        if (document.AccessRights.CanUpdate())
          Functions.ApprovalTask.SetDocumentStateAborted(_obj, setObsolete);
        else
          needSetState = true;
        
        var needGrantAccessRightsOnDocument = Functions.ApprovalTask.NeedGrantAccessRightsOnDocument(_obj);
        if (needSetState || needGrantAccessRightsOnDocument)
          Functions.ApprovalTask.SetDocumentStateAbortedAsync(_obj, setObsolete, needSetState, needGrantAccessRightsOnDocument);
      }
      
      // Прекратить процессы, запущенные сценариями.
      Functions.ApprovalTask.AbortPassedFunctionStages(_obj);
    }

    public override void BeforeStart(Sungero.Workflow.Server.BeforeStartEventArgs e)
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (document == null)
        return;
      
      if (!Sungero.Docflow.Functions.ApprovalTask.ValidateApprovalTaskStart(_obj, e))
        return;
      
      var refreshParameters = Functions.ApprovalTask.GetFullStagesInfoForRefresh(_obj);
      Functions.ApprovalTask.SetRefreshParams(_obj, (Domain.Shared.IExtendedEntity)_obj, refreshParameters);
      // Могли измениться условия, влияющие на обязательность полей.
      Functions.ApprovalTask.SetRequiredProperties(_obj, refreshParameters);
      
      // Сбросить номер текущего этапа при старте и рестарте.
      _obj.StageNumber = null;
      
      // Апнуть Iteration.
      _obj.Iteration++;
      
      // Синхронизация приложений для заполнения коллекции добавленных и удаленных вручную документов.
      Functions.ApprovalTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      
      // Обновить обязательных согласующих.
      Functions.ApprovalTask.UpdateReglamentApprovers(_obj, _obj.ApprovalRule);
      
      // Развернуть дополнительных согласующих.
      var expandedAddApprovers = new List<IRecipient>();
      _obj.AddApproversExpanded.Clear();
      foreach (var addApprover in _obj.AddApprovers.Select(a => a.Approver).ToList())
      {
        if (!Groups.Is(addApprover))
          expandedAddApprovers.Add(addApprover);
        else
        {
          var employees = Groups.GetAllUsersInGroup(Groups.As(addApprover))
            .Where(r => r.Status == CoreEntities.DatabookEntry.Status.Active)
            .Select(r => Recipients.As(r));
          expandedAddApprovers.AddRange(employees);
        }
      }
      foreach (var addApproverEx in expandedAddApprovers.Distinct().ToList())
        _obj.AddApproversExpanded.AddNew().Approver = addApproverEx;
      
      // Заполнить deliveryMethod документа значением из задачи.
      var outgoingDocument = OutgoingDocumentBases.As(document);
      if (_obj.State.Properties.DeliveryMethod.IsVisible == true && outgoingDocument != null && outgoingDocument.IsManyAddressees != true)
        document.DeliveryMethod = _obj.DeliveryMethod;
      // Заполнить статус согласования документа на момент старта задачи.
      _obj.DocumentExternalApprovalState = document.ExternalApprovalState ?? ApprovalTask.DocumentExternalApprovalState.Empty;
      
      // Сбросить значение свойства "Исполнитель этапа не определен".
      _obj.IsStageAssigneeNotFound = false;
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.NeedsReview = false;
      _obj.Iteration = 0;
      if (!_obj.State.IsCopied)
      {
        _obj.Subject = Docflow.Resources.AutoformatTaskSubject;
        
        using (TenantInfo.Culture.SwitchTo())
          _obj.ActiveText = ApprovalTasks.Resources.ApprovalText;
      }
    }
  }
}