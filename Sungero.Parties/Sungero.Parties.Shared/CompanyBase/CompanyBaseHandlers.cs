using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.CompanyBase;

namespace Sungero.Parties
{
  partial class CompanyBaseSharedHandlers
  {

    public virtual void LegalNameChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      
    }
    
    public override void NameChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      base.NameChanged(e);
      
      // Заполнить юрид. наименование по умолчанию наименованием.
      if (!string.IsNullOrWhiteSpace(e.NewValue) && string.IsNullOrWhiteSpace(_obj.LegalName))
        _obj.LegalName = e.NewValue;
    }
  }

}