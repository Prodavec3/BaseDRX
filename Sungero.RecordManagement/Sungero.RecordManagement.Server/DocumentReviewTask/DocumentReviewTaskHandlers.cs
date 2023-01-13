using System.Linq;
using Sungero.Core;

namespace Sungero.RecordManagement
{
  partial class DocumentReviewTaskResolutionObserversObserverPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ResolutionObserversObserverFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return (IQueryable<T>)PublicFunctions.Module.ObserversFiltering(query);
    }
  }

  partial class DocumentReviewTaskCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.ResolutionText);
    }
  }

  partial class DocumentReviewTaskServerHandlers
  {

    public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
    {
      var taskIsCompleted = Functions.DocumentReviewTask.IsDocumentReviewTaskCompleted(_obj);
      if (taskIsCompleted)
      {
        Functions.DocumentReviewTask.ExecuteParentApprovalTaskMonitorings(_obj);
        Functions.DocumentReviewTask.ExecuteParentDocumentReviewTaskMonitorings(_obj);
      }
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // Выдать права на документы для всех, кому выданы права на задачу.
      // Выдать права по каждой группе в отдельности, так как AllAttachments включает в себя удаленные до сохранения документы. Bug 181206.
      if (_obj.State.IsChanged)
      {
        var allAttachments = _obj.DocumentForReviewGroup.All.ToList();
        allAttachments.AddRange(_obj.AddendaGroup.All);
        allAttachments.AddRange(_obj.OtherGroup.All);
        allAttachments.AddRange(_obj.ResolutionGroup.All);
        Docflow.PublicFunctions.Module.GrantManualReadRightForAttachments(_obj, allAttachments);
      }
    }
    
    public override void BeforeRestart(Sungero.Workflow.Server.BeforeRestartEventArgs e)
    {
      // Заполнить коллекции добавленных и удаленных вручную документов в задаче.
      Functions.DocumentReviewTask.AddedAddendaAppend(_obj);
      Functions.DocumentReviewTask.RemovedAddendaAppend(_obj);
      
      // Синхронизация приложений для заполнения коллекции добавленных и удаленных вручную документов.
      Functions.DocumentReviewTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      
      var startedResolutionProjects = _obj.ResolutionGroup.ActionItemExecutionTasks.Where(a => a.IsDraftResolution != true).ToList();
      foreach (var project in startedResolutionProjects)
        _obj.ResolutionGroup.ActionItemExecutionTasks.Remove(project);
    }
    
    public override void BeforeAbort(Sungero.Workflow.Server.BeforeAbortEventArgs e)
    {
      // Если прекращен черновик, прикладную логику по прекращению выполнять не надо.
      if (_obj.State.Properties.Status.OriginalValue == Workflow.Task.Status.Draft)
        return;
      
      // Рекурсивно прекратить подзадачи.
      Functions.DocumentReviewTask.AbortDocumentReviewSubTasks(_obj);
      
      // Обновить статус исполнения - пустой.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      Functions.Module.SetDocumentExecutionState(_obj, document, null);
      Functions.Module.SetDocumentControlExecutionState(document);
    }

    public override void BeforeStart(Sungero.Workflow.Server.BeforeStartEventArgs e)
    {
      if (!Sungero.RecordManagement.Functions.DocumentReviewTask.ValidateDocumentReviewTaskStart(_obj, e))
        return;
      
      // Сброс отметок о создании подзадач для нескольких адресатов.
      foreach (var addressee in _obj.Addressees)
        addressee.TaskCreated = false;
      
      // Обновить адресата в задаче-контейнере, если он изменен при рестарте.
      // В одиночном адресате - старое значение, в коллекции новое.
      if (_obj.Addressees.Count == 1 && !Equals(_obj.Addressee, _obj.Addressees.First().Addressee))
      {
        var parentTask = Functions.Module.GetParentTask(_obj);
        if (parentTask != null && DocumentReviewTasks.Is(parentTask))
        {
          var lockInfo = Locks.GetLockInfo(parentTask);
          if (!lockInfo.IsLockedByOther)
          {
            var documentReviewTask = DocumentReviewTasks.As(parentTask);
            var addressee = documentReviewTask.Addressees.FirstOrDefault(x => Equals(x.Addressee, _obj.Addressee));
            if (addressee != null)
              addressee.Addressee = _obj.Addressees.First().Addressee;
          }
          else
          {
            Logger.DebugFormat("DocumentReviewTask({0}): cannot synchronize addressee. Parent task ({1}) is locked by {2}.", _obj.Id, parentTask.Id, lockInfo.OwnerName);
          }
        }
      }
      
      // Для корректной работы изначальной логики с одним адресатом.
      if (_obj.Addressees.Count == 1)
        _obj.Addressee = _obj.Addressees.First().Addressee;
      
      // Выдать права группе регистрации документа.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      if (document.DocumentRegister != null)
      {
        var registrationGroup = document.DocumentRegister.RegistrationGroup;
        if (registrationGroup != null)
          _obj.AccessRights.Grant(registrationGroup, DefaultAccessRightsTypes.Change);
      }
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      
      // Получить ресурсы в культуре тенанта.
      using (TenantInfo.Culture.SwitchTo())
      {
        if (document != null)
          _obj.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.Consideration, document.Name);
        else
          _obj.Subject = Docflow.Resources.AutoformatTaskSubject;
        
        if (!_obj.State.IsCopied)
          _obj.ActiveText = Resources.ConsiderationText;
      }
      
      _obj.NeedsReview = false;
    }
  }
}