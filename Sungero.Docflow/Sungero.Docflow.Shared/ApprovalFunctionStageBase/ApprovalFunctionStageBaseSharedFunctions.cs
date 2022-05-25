using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalFunctionStageBase;

namespace Sungero.Docflow.Shared
{
  partial class ApprovalFunctionStageBaseFunctions
  {

    public override Enumeration? GetStageType()
    {
      return Docflow.ApprovalRuleBaseStages.StageType.Function;
    }
  }
}