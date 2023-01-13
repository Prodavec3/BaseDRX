using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.StampSetting;

namespace Sungero.Docflow.Shared
{
  partial class StampSettingFunctions
  {
    /// <summary>
    /// Получить текст ошибки о наличии дублей.
    /// </summary>
    /// <returns>Текст ошибки или пустая строка, если ошибок нет.</returns>
    public virtual string GetDuplicatesErrorText()
    {
      var duplicates = this.GetDuplicates();
      
      if (!duplicates.Any())
        return string.Empty;
      
      // Сформировать текст ошибки.
      return StampSettings.Resources.DuplicatesDetected;
    }
    
    /// <summary>
    /// Получить дубли настройки отметки в документах.
    /// </summary>
    /// <returns>Дубли настройки отметки в документах.</returns>
    public virtual List<IStampSetting> GetDuplicates()
    {
      return Functions.StampSetting.Remote.GetStampSettingDuplicates(_obj);
    }
  }
}