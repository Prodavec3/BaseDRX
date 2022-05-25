using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.OutgoingDocumentBase;

namespace Sungero.Docflow
{
  partial class OutgoingDocumentBaseAddresseesClientHandlers
  {

    public virtual void AddresseesNumberValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      // Проверить число на положительность.
      if (e.NewValue < 1)
        e.AddError(Resources.NumberDistributionListIsNotPositive);
    }
  }

  partial class OutgoingDocumentBaseClientHandlers
  {
    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      if (_obj.IsManyAddressees == true)
      {
        _obj.State.Properties.Correspondent.IsVisible = false;
        _obj.State.Properties.Correspondent.IsRequired = false;
        _obj.State.Properties.DistributionCorrespondent.IsVisible = true;
        _obj.State.Properties.Addressee.IsEnabled = false;
        _obj.State.Properties.DeliveryMethod.IsEnabled = false;
        
        _obj.State.Properties.Addressees.IsEnabled = true;
        _obj.State.Properties.Addressees.Properties.Addressee.IsEnabled = !Functions.OutgoingDocumentBase.DisableAddresseesOnRegistration(_obj, e);
      }
      else
      {
        _obj.State.Properties.Correspondent.IsRequired = true;
        _obj.State.Properties.Correspondent.IsVisible = true;
        _obj.State.Properties.DistributionCorrespondent.IsVisible = false;
        _obj.State.Properties.Addressee.IsEnabled = !Functions.OutgoingDocumentBase.DisableAddresseesOnRegistration(_obj, e) &&
          (_obj.Correspondent == null || Sungero.Parties.CompanyBases.Is(_obj.Correspondent));
        _obj.State.Properties.DeliveryMethod.IsEnabled = true;
        
        _obj.State.Properties.Addressees.IsEnabled = false;
      }
    }
  }
}