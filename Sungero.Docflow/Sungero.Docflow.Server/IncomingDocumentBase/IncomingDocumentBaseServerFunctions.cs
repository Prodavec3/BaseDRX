using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.IncomingDocumentBase;

namespace Sungero.Docflow.Server
{
  partial class IncomingDocumentBaseFunctions
  {
    /// <summary>
    /// Заполнить текстовое отображение адресатов.
    /// </summary>
    public virtual void SetManyAddresseesLabel()
    {
      var addressees = _obj.Addressees
        .Where(x => x.Addressee != null)
        .Select(x => x.Addressee)
        .ToList();
      var maxLength = _obj.Info.Properties.ManyAddresseesLabel.Length;
      var label = Functions.Module.BuildManyAddresseesLabel(addressees, maxLength);
      if (_obj.ManyAddresseesLabel != label)
        _obj.ManyAddresseesLabel = label;
    }
  }
}