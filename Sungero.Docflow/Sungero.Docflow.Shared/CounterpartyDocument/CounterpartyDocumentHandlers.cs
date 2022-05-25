using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.CounterpartyDocument;

namespace Sungero.Docflow
{
  partial class CounterpartyDocumentSharedHandlers
  {

    public virtual void CounterpartyChanged(Sungero.Docflow.Shared.CounterpartyDocumentCounterpartyChangedEventArgs e)
    {
      this.FillName();
    }

    public override void DocumentKindChanged(Sungero.Docflow.Shared.OfficialDocumentDocumentKindChangedEventArgs e)
    {
      base.DocumentKindChanged(e);
      
      if (e.NewValue != null && e.NewValue.NumberingType != Docflow.DocumentKind.NumberingType.NotNumerable)
      {
        if (_obj.BusinessUnit == null)
          _obj.BusinessUnit = Docflow.PublicFunctions.Module.GetDefaultBusinessUnit(Company.Employees.Current);
        
        _obj.State.Properties.BusinessUnit.IsVisible = true;
        _obj.State.Properties.Department.IsVisible = true;
      }
      else
      {
        _obj.State.Properties.BusinessUnit.IsVisible = false;
        _obj.State.Properties.Department.IsVisible = false;
      }
    }

  }
}