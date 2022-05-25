using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalRoleBase;

namespace Sungero.Docflow
{
  partial class ApprovalRoleBaseServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e) 
    {
      _obj.Name = _obj.Info.Properties.Type.GetLocalizedValue(_obj.Type);
    }
  }

}