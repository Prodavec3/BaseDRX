using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.BusinessUnitBox;

namespace Sungero.ExchangeCore
{
  partial class BusinessUnitBoxClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      if (!string.IsNullOrWhiteSpace(_obj.OrganizationId))
      {
        _obj.State.Properties.BusinessUnit.IsEnabled = false;
        _obj.State.Properties.ExchangeService.IsEnabled = false;
      }
    }

    public override void ResponsibleValueInput(Sungero.ExchangeCore.Client.BoxBaseResponsibleValueInputEventArgs e)
    {
      base.ResponsibleValueInput(e);
      
      if (e.NewValue != null && !Functions.BusinessUnitBox.Remote.CheckAllResponsibleCertificates(_obj, e.NewValue))
        e.AddWarning(_obj.Info.Properties.Responsible, BusinessUnitBoxes.Resources.CertificateNotFound, _obj.Info.Properties.Responsible);
    }
  }

}