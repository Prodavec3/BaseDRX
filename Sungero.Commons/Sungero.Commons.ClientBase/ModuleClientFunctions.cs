using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons.Client
{
  public class ModuleFunctions
  {    
    /// <summary>
    /// Создать населенный пункт.
    /// </summary>
    public virtual void CreateNewCity()
    {
      Functions.Module.Remote.CreateNewCity().Show();
    }
  }
}