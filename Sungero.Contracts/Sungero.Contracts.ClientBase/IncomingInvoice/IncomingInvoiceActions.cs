using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.IncomingInvoice;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.FinancialArchive;

namespace Sungero.Contracts.Client
{
  partial class IncomingInvoiceCollectionActions
  {
    public override void ExportFinancialDocument(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ExportFinancialDocument(e);
    }

    public override bool CanExportFinancialDocument(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

  }

  partial class IncomingInvoiceActions
  {

    public virtual void ShowDuplicates(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var duplicates = Functions.IncomingInvoice.Remote.GetDuplicates(_obj,
                                                                      _obj.DocumentKind,
                                                                      _obj.Number,
                                                                      _obj.Date,
                                                                      _obj.TotalAmount,
                                                                      _obj.Currency,
                                                                      _obj.Counterparty);
      if (duplicates.Any())
        duplicates.Show();
      else
        Dialogs.NotifyMessage(IncomingInvoices.Resources.DuplicateNotFound);
    }

    public virtual bool CanShowDuplicates(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public override void ReturnDocument(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ReturnDocument(e);
    }

    public override bool CanReturnDocument(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate() &&
        _obj.IsReturnRequired == true;
    }

  }

}