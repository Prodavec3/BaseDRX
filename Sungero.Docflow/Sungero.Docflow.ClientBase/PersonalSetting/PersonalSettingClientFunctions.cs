using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PersonalSetting;

namespace Sungero.Docflow.Client
{
  partial class PersonalSettingFunctions
  {
    
    /// <summary>
    /// Настроить видимость координат отметки о поступлении.
    /// </summary>
    /// <param name="position">Расположение отметки.</param>
    /// <remarks>Координаты появляются при выборе произвольного расположения отметки ("По координатам").</remarks>
    public virtual void ChangeRegistrationStampCoordsVisibility(Enumeration? position)
    {
      var properties = _obj.State.Properties;
      
      if (position != null && position == RegistrationStampPosition.Custom)
      {
        properties.RightIndent.IsVisible = true;
        properties.BottomIndent.IsVisible = true;
        properties.RightIndent.IsRequired = true;
        properties.BottomIndent.IsRequired = true;
      }
      else
      {
        properties.RightIndent.IsVisible = false;
        properties.BottomIndent.IsVisible = false;
        properties.RightIndent.IsRequired = false;
        properties.BottomIndent.IsRequired = false;
      }
    }
    
  }
}