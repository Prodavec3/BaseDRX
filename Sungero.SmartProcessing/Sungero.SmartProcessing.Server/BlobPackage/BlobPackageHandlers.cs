using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.SmartProcessing.BlobPackage;

namespace Sungero.SmartProcessing
{
  partial class BlobPackageServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.ProcessState = ProcessState.InProcess;
    }
  }

}