using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.AcquaintanceTask;

namespace Sungero.RecordManagement
{
  partial class AcquaintanceTaskExcludedPerformersExcludedPerformerPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ExcludedPerformersExcludedPerformerFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return (IQueryable<T>)Functions.Module.ObserversFiltering(query);
    }
  }

  partial class AcquaintanceTaskObserversObserverPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> ObserversObserverFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return (IQueryable<T>)Functions.Module.ObserversFiltering(query);
    }
  }

  partial class AcquaintanceTaskPerformersPerformerPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> PerformersPerformerFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return (IQueryable<T>)Functions.Module.ObserversFiltering(query);
    }
  }

  partial class AcquaintanceTaskCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.Deadline);
    }
  }

  partial class AcquaintanceTaskServerHandlers
  {

    public override void BeforeAbort(Sungero.Workflow.Server.BeforeAbortEventArgs e)
    {
      
    }

    public override void AfterDelete(Sungero.Domain.AfterDeleteEventArgs e)
    {
      // Удалить список участников ознакомления, соответствующий задаче.
      if (_obj != null)
      {
        var participants = AcquaintanceTaskParticipants.GetAll().FirstOrDefault(x => x.TaskId == _obj.Id);
        if (participants != null)
          AcquaintanceTaskParticipants.Delete(participants);
      }
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      
    }

    public override void BeforeStart(Sungero.Workflow.Server.BeforeStartEventArgs e)
    {
      if (!Sungero.RecordManagement.Functions.AcquaintanceTask.ValidateAcquaintanceTaskStart(_obj, e))
        return;
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.IsElectronicAcquaintance = true;
      
      if (!_obj.State.IsCopied)
      {
        _obj.ReceiveOnCompletion = ReceiveOnCompletion.Assignment;
        _obj.Subject = Docflow.Resources.AutoformatTaskSubject;
        
        // Получить ресурсы в культуре тенанта.
        using (TenantInfo.Culture.SwitchTo())
          _obj.ActiveText = AcquaintanceTasks.Resources.TaskAutoText;
      }
    }

    public override void BeforeRestart(Sungero.Workflow.Server.BeforeRestartEventArgs e)
    {
      // Очистить таблицу с номером версии и хешем документа.
      _obj.AcquaintanceVersions.Clear();
      var participants = AcquaintanceTaskParticipants.GetAll().FirstOrDefault(x => x.TaskId == _obj.Id);
      if (participants != null)
        participants.Employees.Clear();
      
      Functions.AcquaintanceTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
    }
  }

}