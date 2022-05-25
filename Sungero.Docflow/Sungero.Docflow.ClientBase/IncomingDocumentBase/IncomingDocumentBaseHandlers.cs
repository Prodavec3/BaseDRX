using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.IncomingDocumentBase;

namespace Sungero.Docflow
{
  partial class IncomingDocumentBaseAddresseesClientHandlers
  {

    public virtual void AddresseesNumberValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      // Проверить число на положительность.
      if (e.NewValue < 1)
        e.AddError(Sungero.Docflow.OfficialDocuments.Resources.NumberAddresseeListIsNotPositive);
    }
  }

  partial class IncomingDocumentBaseClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      if (_obj.IsManyAddressees.HasValue)
      {
        _obj.State.Properties.Addressee.IsVisible = !_obj.IsManyAddressees.Value;
        _obj.State.Properties.Addressee.IsEnabled = !_obj.IsManyAddressees.Value;
        _obj.State.Properties.ManyAddresseesPlaceholder.IsVisible = _obj.IsManyAddressees.Value;
        _obj.State.Properties.Addressees.IsEnabled = _obj.IsManyAddressees.Value;
      }
    }

    public virtual void DatedValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if (e.NewValue != null && e.NewValue < Calendar.SqlMinValue)
        e.AddError(_obj.Info.Properties.Dated, Sungero.Docflow.OfficialDocuments.Resources.SetCorrectDate);
    }

  }
}