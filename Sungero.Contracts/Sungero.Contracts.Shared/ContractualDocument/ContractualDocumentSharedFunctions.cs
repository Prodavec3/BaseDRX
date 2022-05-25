using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts.Shared
{
  partial class ContractualDocumentFunctions
  {
    /// <summary>
    /// Получить ответственного за документ.
    /// </summary>
    /// <returns>Пользователь, ответственный за документ.</returns>
    public override Company.IEmployee GetDocumentResponsibleEmployee()
    {
      if (_obj.ResponsibleEmployee != null)
        return _obj.ResponsibleEmployee;
      
      return base.GetDocumentResponsibleEmployee();
    }
  }
}