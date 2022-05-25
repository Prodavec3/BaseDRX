using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.MessageQueueItem;

namespace Sungero.ExchangeCore.Client
{
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

    public virtual void Resume(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      _obj.ProcessingStatus = ExchangeCore.MessageQueueItem.ProcessingStatus.NotProcessed;
      _obj.Save();
    }

    public virtual bool CanResume(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Equals(_obj.ProcessingStatus, ExchangeCore.MessageQueueItem.ProcessingStatus.Suspended) && _obj.AccessRights.CanUpdate();
    }

    public virtual void DoSuspended(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      _obj.ProcessingStatus = ExchangeCore.MessageQueueItem.ProcessingStatus.Suspended;
      _obj.Save();
    }

    public virtual bool CanDoSuspended(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !Equals(_obj.ProcessingStatus, ExchangeCore.MessageQueueItem.ProcessingStatus.Suspended) && _obj.AccessRights.CanUpdate();
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