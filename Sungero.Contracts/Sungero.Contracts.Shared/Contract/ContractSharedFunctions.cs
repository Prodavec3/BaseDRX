using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.Contract;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;

namespace Sungero.Contracts.Shared
{
  partial class ContractFunctions
  {
    
    #region Интеллектуальная обработка
    
    [Public]
    public override bool IsVerificationModeSupported()
    {
      return true;
    }
    
    #endregion
    
  }
}