using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.StampSetting;

namespace Sungero.Docflow
{
  partial class StampSettingClientHandlers
  {

    public virtual void NameValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (!string.IsNullOrEmpty(e.NewValue))
        e.NewValue = e.NewValue.Trim();
    }

    public virtual void TitleValueInput(Sungero.Presentation.TextValueInputEventArgs e)
    {
      // Максимальная длина заголовка отметки об ЭП с учетом размера отметки по ГОСТу.
      const int MaxTitleLength = 250;
      
      if (!string.IsNullOrEmpty(e.NewValue))
      {
        e.NewValue = e.NewValue.Trim();
        if (e.NewValue.Length > MaxTitleLength)
          e.AddError(StampSettings.Resources.TooLongTitle);
      }
    }

  }
}