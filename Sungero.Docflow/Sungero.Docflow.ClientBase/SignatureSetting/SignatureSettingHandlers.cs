using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.SignatureSetting;

namespace Sungero.Docflow
{

  partial class SignatureSettingClientHandlers
  {

    public virtual IEnumerable<Enumeration> ReasonFiltering(IEnumerable<Enumeration> query)
    {
      if (_obj.Recipient != null && !Sungero.Company.Employees.Is(_obj.Recipient))
        query = query.Where(x => x != Sungero.Docflow.SignatureSetting.Reason.FormalizedPoA && 
                                 x != Sungero.Docflow.SignatureSetting.Reason.PowerOfAttorney);
      return query;
    }

    public virtual void SigningReasonValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (!string.IsNullOrEmpty(e.NewValue))
        e.NewValue = e.NewValue.Trim();
    }

    public virtual void DocumentValueInput(Sungero.Docflow.Client.SignatureSettingDocumentValueInputEventArgs e)
    {
      if (e.NewValue != null && _obj.Reason == Docflow.SignatureSetting.Reason.PowerOfAttorney && !Docflow.PowerOfAttorneys.Is(e.NewValue))
        e.AddError(SignatureSettings.Info.Properties.Document, Docflow.SignatureSettings.Resources.IncorrectDocumentType);
    }

    public virtual void ReasonValueInput(Sungero.Presentation.EnumerationValueInputEventArgs e)
    {
      _obj.State.Properties.Document.IsEnabled = e.NewValue != Docflow.SignatureSetting.Reason.Duties;
      _obj.State.Properties.Document.IsRequired = e.NewValue == Docflow.SignatureSetting.Reason.PowerOfAttorney ||
        e.NewValue == Docflow.SignatureSetting.Reason.FormalizedPoA;
      _obj.State.Properties.ValidTill.IsRequired = e.NewValue == Docflow.SignatureSetting.Reason.PowerOfAttorney ||
        e.NewValue == Docflow.SignatureSetting.Reason.FormalizedPoA;
      _obj.State.Properties.Certificate.IsRequired = e.NewValue == Docflow.SignatureSetting.Reason.FormalizedPoA;
    }

    public virtual void AmountValueInput(Sungero.Presentation.DoubleValueInputEventArgs e)
    {
      if (e.NewValue < 0)
        e.AddError(SignatureSettings.Resources.NegativeAmount);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      var categories = Functions.SignatureSetting.GetPossibleCashedCategories(_obj);
      _obj.State.Properties.Categories.IsEnabled = categories.Any();
      
      Functions.SignatureSetting.ChangePropertiesAccess(_obj);
    }

  }
}