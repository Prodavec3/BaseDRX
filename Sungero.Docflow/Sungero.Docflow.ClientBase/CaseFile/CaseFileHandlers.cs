using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.CaseFile;

namespace Sungero.Docflow
{
  partial class CaseFileClientHandlers
  {

    public virtual void IndexValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (string.IsNullOrWhiteSpace(e.NewValue) || e.NewValue == e.OldValue)
        return;
      
      // Использование пробелов в середине индекса запрещено.
      var newIndex = e.NewValue.Trim();
      if (Regex.IsMatch(newIndex, @"\s"))
        e.AddError(Docflow.CaseFiles.Resources.NoSpacesInIndex);
    }
    
    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (!e.IsValid)
        return;
      
      if (_obj.AccessRights.CanUpdate() && !_obj.State.IsInserted && _obj.RegistrationGroup != null &&
          !Functions.Module.CalculateParams(e, _obj.RegistrationGroup, true, true, false, false, null))
        foreach (var property in _obj.State.Properties)
          property.IsEnabled = false;
    }
  }
}