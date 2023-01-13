using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.ContractualDocument;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts
{
  partial class ContractualDocumentMilestonesClientHandlers
  {

    public virtual void MilestonesDaysToFinishWorksValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (_obj.IsCompleted.Value)
        e.AddError(ContractualDocuments.Resources.CannotChangeCompleteMilestone);
      if (e.NewValue <= 0)
        e.AddError(ContractualDocuments.Resources.IncorrectFinishWorksDay);
    }

    public virtual void MilestonesNoteValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (_obj.IsCompleted.Value)
        e.AddError(ContractualDocuments.Resources.CannotChangeCompleteMilestone);
    }

    public virtual void MilestonesPerformerValueInput(Sungero.Contracts.Client.ContractualDocumentMilestonesPerformerValueInputEventArgs e)
    {
      if (_obj.IsCompleted.Value)
        e.AddError(ContractualDocuments.Resources.CannotChangeCompleteMilestone);
      if (_obj.Task != null)
        e.AddError(ContractualDocuments.Resources.CannotChangePerformer);
      
      // Проверить корректность срока.      
      var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(e.NewValue ?? Users.Current, _obj.Deadline);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage); 
    }

    public virtual void MilestonesDeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if (_obj.IsCompleted.Value)
        e.AddError(ContractualDocuments.Resources.CannotChangeCompleteMilestone);
      
      // Проверить корректность срока.      
      var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(_obj.Performer ?? Users.Current, e.NewValue);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);   
      
      if (!Docflow.PublicFunctions.Module.CheckDeadline(_obj.Performer ?? Users.Current, e.NewValue, Calendar.Today))
        e.AddWarning(ContractualDocuments.Resources.DeadlineMilestoneLessThenToday);      
    }

    public virtual void MilestonesNameValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (_obj.IsCompleted.Value)
        e.AddError(ContractualDocuments.Resources.CannotChangeCompleteMilestone);
    }
  }

  partial class ContractualDocumentClientHandlers
  {

    public override void TotalAmountValueInput(Sungero.Presentation.DoubleValueInputEventArgs e)
    {
      if (e.NewValue <= 0)
        e.AddError(ContractualDocuments.Resources.TotalAmountMustBePositive);
      
      base.TotalAmountValueInput(e);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      var isCompany = Sungero.Parties.CompanyBases.Is(_obj.Counterparty) || _obj.Counterparty == null;

      _obj.State.Properties.Contact.IsEnabled = isCompany;
      _obj.State.Properties.CounterpartySignatory.IsEnabled = isCompany;
    }

  }
}