using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.InternalDocumentBase;
using Sungero.Reporting;

namespace Sungero.Docflow.Client
{
  partial class InternalDocumentBaseActions
  {
    public virtual void OpenActionItems(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.Remote.GetActionItemsByDocument(_obj).Show();
    }

    public virtual bool CanOpenActionItems(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

    public virtual void OpenActionItemExecutionReport(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var actionItemsArePresent = RecordManagement.PublicFunctions.Module.Remote.ActionItemCompletionDataIsPresent(null, _obj);
      
      if (!actionItemsArePresent)
      {
        Dialogs.NotifyMessage(RecordManagement.Reports.Resources.ActionItemsExecutionReport.NoAnyActionItemsForDocument);
        return;
      }
      else
      {
      var actionItemExecutionReport = RecordManagement.Reports.GetActionItemsExecutionReport();
      actionItemExecutionReport.Document = _obj;
      actionItemExecutionReport.Open();
      }
    }

    public virtual bool CanOpenActionItemExecutionReport(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

  }

  internal static class InternalDocumentBaseStaticActions
  {
    public static bool CanDocumentRegister(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return RecordManagement.PublicFunctions.Module.GetInternalDocumentsReport().CanExecute();
    }

    public static void DocumentRegister(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      RecordManagement.PublicFunctions.Module.GetInternalDocumentsReport().Open();
    }
  }

}