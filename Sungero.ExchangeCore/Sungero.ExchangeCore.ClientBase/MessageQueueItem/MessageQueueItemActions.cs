using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.MessageQueueItem;

namespace Sungero.ExchangeCore.Client
{
  partial class MessageQueueItemCollectionActions
  {

    public virtual bool CanDoSuspended(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _objs.Any() && _objs.Any(x => !Equals(x.ProcessingStatus, ExchangeCore.MessageQueueItem.ProcessingStatus.Suspended));
    }

    public virtual void DoSuspended(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var hasErrors = false;
      foreach (var obj in _objs)
      {
        try
        {
          if (!Equals(obj.ProcessingStatus, ExchangeCore.MessageQueueItem.ProcessingStatus.Suspended))
          {
            obj.ProcessingStatus = ExchangeCore.MessageQueueItem.ProcessingStatus.Suspended;
            obj.Save();
          }
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("Failed to stop processing queue item {0}", ex, obj.Id);
          hasErrors = true;
          continue;
        }
      }
      if (hasErrors)
        Dialogs.NotifyMessage(Sungero.ExchangeCore.MessageQueueItems.Resources.DoSuspendedError);
    }

    public virtual bool CanResume(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _objs.Any() && _objs.Any(x => Equals(x.ProcessingStatus, ExchangeCore.MessageQueueItem.ProcessingStatus.Suspended));
    }

    public virtual void Resume(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var hasErrors = false;
      foreach (var obj in _objs)
      {
        try
        {
          if (Equals(obj.ProcessingStatus, ExchangeCore.MessageQueueItem.ProcessingStatus.Suspended))
          {
            obj.ProcessingStatus = ExchangeCore.MessageQueueItem.ProcessingStatus.NotProcessed;
            obj.Save();
          }
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("Failed to resume processing queue item {0}", ex, obj.Id);
          hasErrors = true;
          continue;
        }
      }
      if (hasErrors)
        Dialogs.NotifyMessage(Sungero.ExchangeCore.MessageQueueItems.Resources.ResumeError);
    }
  }

  partial class MessageQueueItemActions
  {
    public override void DeleteEntity(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.DeleteEntity(e);
    }

    public override bool CanDeleteEntity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public virtual void OpenInExchangeService(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      try
      {
        var hyperlink = Sungero.Exchange.PublicFunctions.Module.Remote.GetDocumentHyperlink(_obj);
        if (string.IsNullOrWhiteSpace(hyperlink))
          e.AddInformation(Docflow.OfficialDocuments.Resources.DocumentNotInService);
        else
          Hyperlinks.Open(hyperlink);
      }
      catch (AppliedCodeException ex)
      {
        e.AddError(ex.Message);
      }
    }

    public virtual bool CanOpenInExchangeService(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }

}