using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Company.Department;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company
{
  partial class DepartmentClientHandlers
  {

    public virtual void CodeValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (string.IsNullOrWhiteSpace(e.NewValue) || e.NewValue == e.OldValue)
        return;
      
      // Использование пробелов в середине кода запрещено.
      var newCode = e.NewValue.Trim();
      if (Regex.IsMatch(newCode, @"\s"))
        e.AddError(Company.Resources.NoSpacesInCode);
    }

  }
}