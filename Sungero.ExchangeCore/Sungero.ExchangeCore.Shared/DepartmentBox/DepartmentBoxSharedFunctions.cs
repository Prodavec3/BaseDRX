using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.DepartmentBox;

namespace Sungero.ExchangeCore.Shared
{
  partial class DepartmentBoxFunctions
  {
    /// <summary>
    /// Сформировать имя ящика подразделения.
    /// </summary>
    public void SetDepartmentBoxName()
    {
      var name  = string.Format("{0} - {1}", _obj.ParentBox, _obj.ServiceName);
      if (name.Length > _obj.Info.Properties.Name.Length)
        name = name.Remove(_obj.Info.Name.Length);
      _obj.Name = name;
    }
    
    /// <summary>
    /// Получить основной ящик.
    /// </summary>
    /// <returns>Основной ящик.</returns>
    [Public]
    public override IBusinessUnitBox GetRootBox()
    {
      return _obj.RootBox;
    }
  }
}